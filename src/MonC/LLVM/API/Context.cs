using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MonC.TypeSystem;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    /// <summary>
    /// Exception triggered by LLVM's diagnostic handler.
    ///
    /// Note: LLVM is typically compiled without exception handling enabled
    /// so it will not undergo stack unwinding if caught.
    /// Therefore, it is not recommended to catch this exception type frequently.
    /// </summary>
    class LLVMException : Exception
    {
        public LLVMException(string message) : base(message) { }
    }

    public sealed class Context : IDisposable
    {
        private LLVMContextRef _context;

        private GCHandle _diagnosticHandlerHandle;
        private uint _childModuleCount;

        public static implicit operator LLVMContextRef(Context context) => context._context;

        public Context()
        {
            _context = LLVMContextRef.Create();
            VoidType = _context.VoidType;
            Int1Type = _context.Int1Type;
            Int8Type = _context.Int8Type;
            Int16Type = _context.Int16Type;
            Int32Type = _context.Int32Type;
            Int64Type = _context.Int64Type;
            SetDiagnosticHandler(diagnosticInfo =>
                Console.WriteLine($"LLVM {diagnosticInfo.Severity}: {diagnosticInfo.DescriptionString}"));
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_context.Handle != IntPtr.Zero) {
                if (_childModuleCount != 0) {
                    throw new InvalidOperationException("Live child modules are still present");
                }

                if (_diagnosticHandlerHandle.IsAllocated)
                    _diagnosticHandlerHandle.Free();

                _context.Dispose();
            }
        }

        ~Context() => DoDispose();

        public Module CreateModule(string name) => new Module(name, this);
        public Builder CreateBuilder() => new Builder(_context);

        public Module ParseIR(MemoryBuffer memBuf)
        {
            try {
                LLVMModuleRef moduleOut = _context.ParseIR(memBuf);
                return new Module(moduleOut, this);
            } finally {
                // LLVMParseIRInContext always deletes memory buffer in for some reason
                memBuf.Release();
            }
        }

        public Metadata DebugMetadataVersion =>
            Metadata.FromValue(Value.ConstInt(Int32Type, LLVMSharp.Interop.LLVM.DebugMetadataVersion(), false));

        public Type VoidType;
        public Type Int1Type;
        public Type Int8Type;
        public Type Int16Type;
        public Type Int32Type;
        public Type Int64Type;
        public Type IntType(uint numBits) => _context.GetIntType(numBits);

        public Type FunctionType(Type returnType, Type[] paramTypes, bool isVarArg) =>
            LLVMTypeRef.CreateFunction(returnType, Array.ConvertAll(paramTypes, tp => (LLVMTypeRef) tp), isVarArg);

        public Metadata CreateDebugLocation(uint line, uint column, Metadata scope, Metadata inlinedAt) =>
            _context.CreateDebugLocation(line, column, scope, inlinedAt);

        private readonly Dictionary<string, Type> _structs = new Dictionary<string, Type>();

        public Type? LookupPrimitiveType(Primitive primitive)
        {
            return primitive switch {
                Primitive.Void => VoidType,
                Primitive.Int => Int32Type,
                _ => null
            };
        }

        public Type? LookupStructType(string name)
        {
            if (_structs.TryGetValue(name, out Type type))
                return type;
            return null;
        }

        public Type CreateStruct(string name, Type[] elementTypes, bool packed = false)
        {
            if (_structs.ContainsKey(name))
                throw new InvalidOperationException($"struct '{name}' is already defined");
            LLVMTypeRef type = _context.CreateNamedStruct(name);
            type.StructSetBody(Array.ConvertAll(elementTypes, tp => (LLVMTypeRef) tp), packed);
            Type castType = type;
            _structs.Add(name, castType);
            return castType;
        }

        public BasicBlock CreateBasicBlock(string name = "") => _context.CreateBasicBlock(name);

        public BasicBlock AppendBasicBlock(Value fn, string name = "") =>
            _context.AppendBasicBlock(fn, name);

        public BasicBlock InsertBasicBlock(BasicBlock before, string name = "") =>
            _context.InsertBasicBlock(before, name);

        public Value MetadataAsValue(Metadata md) => _context.MetadataAsValue(md);

        public struct DiagnosticInfo
        {
            public LLVMDiagnosticSeverity Severity;
            public string DescriptionString;
        }

        public delegate void DiagnosticHandler(DiagnosticInfo diagnosticInfo);

        public unsafe void SetDiagnosticHandler(DiagnosticHandler handler)
        {
            if (_diagnosticHandlerHandle.IsAllocated)
                _diagnosticHandlerHandle.Free();

            LLVMDiagnosticHandler internalHandler = (diagnosticInfo, diagnosticContext) => {
                DiagnosticInfo info;
                info.Severity = LLVMSharp.Interop.LLVM.GetDiagInfoSeverity(diagnosticInfo);
                info.DescriptionString = MarshaledString.NativeToManaged(
                    LLVMSharp.Interop.LLVM.GetDiagInfoDescription(diagnosticInfo));
                handler(info);
                if (info.Severity == LLVMDiagnosticSeverity.LLVMDSError)
                    throw new LLVMException(info.DescriptionString);
            };
            _diagnosticHandlerHandle = GCHandle.Alloc(internalHandler);

            _context.SetDiagnosticHandler(internalHandler, IntPtr.Zero);

            // Also set this handler in the global context for APIs that do not use a specific context
            LLVMContextRef.Global.SetDiagnosticHandler(internalHandler, IntPtr.Zero);
        }

        internal void IncrementModule() => ++_childModuleCount;
        internal void DecrementModule() => --_childModuleCount;


        public static void Main(string[] args)
        {
            using Context context = new Context();
            using Module module = context.CreateModule("MyModule");
            Type funcType = context.FunctionType(context.Int32Type, new Type[] { }, false);
            Value function = module.AddFunction("GetDragonsBankBalance", funcType);

            BasicBlock basicBlock = context.AppendBasicBlock(function);

            using (Builder builder = context.CreateBuilder()) {
                builder.PositionAtEnd(basicBlock);
                Value theAnswer = Value.ConstInt(context.Int32Type, int.MaxValue, true);
                builder.BuildRet(theAnswer);
            }

            module.Dump();
        }
    }
}
