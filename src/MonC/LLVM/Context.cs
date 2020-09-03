using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
        public LLVMException(string message) : base(message)
        {
        }
    }

    public sealed class Context : IDisposable
    {
        private CAPI.LLVMContextRef _context;
        private GCHandle _diagnosticHandlerHandle;

        public Context()
        {
            _context = CAPI.LLVMContextCreate();
            VoidType = CAPI.LLVMVoidTypeInContext(_context);
            Int1Type = CAPI.LLVMInt1TypeInContext(_context);
            Int8Type = CAPI.LLVMInt8TypeInContext(_context);
            Int16Type = CAPI.LLVMInt16TypeInContext(_context);
            Int32Type = CAPI.LLVMInt32TypeInContext(_context);
            Int64Type = CAPI.LLVMInt64TypeInContext(_context);
            Int128Type = CAPI.LLVMInt128TypeInContext(_context);
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
            if (_context.IsValid) {
                if (_diagnosticHandlerHandle.IsAllocated)
                    _diagnosticHandlerHandle.Free();

                CAPI.LLVMContextDispose(_context);
                _context = new CAPI.LLVMContextRef();
            }
        }

        ~Context() => DoDispose();

        public Module CreateModule(string name, bool debugInfo) => new Module(name, _context, debugInfo);
        public Builder CreateBuilder() => new Builder(_context);

        public Metadata DebugMetadataVersion =>
            Metadata.FromValue(Value.ConstInt(Int32Type, CAPI.LLVMDebugMetadataVersion(), false));

        public Type VoidType;
        public Type Int1Type;
        public Type Int8Type;
        public Type Int16Type;
        public Type Int32Type;
        public Type Int64Type;
        public Type Int128Type;
        public Type IntType(uint numBits) => CAPI.LLVMIntTypeInContext(_context, numBits);

        public Type FunctionType(Type returnType, Type[] paramTypes,
            bool isVarArg) => CAPI.LLVMFunctionType(returnType,
            Array.ConvertAll(paramTypes, tp => (CAPI.LLVMTypeRef) tp), (uint) paramTypes.Length, isVarArg);

        public Metadata CreateDebugLocation(uint line, uint column, Metadata scope, Metadata inlinedAt) =>
            CAPI.LLVMDIBuilderCreateDebugLocation(_context, line, column, scope, inlinedAt);

        private Dictionary<string, Type> _udts = new Dictionary<string, Type>();

        public Type? LookupType(string name)
        {
            if (name.Length == 0)
                return VoidType;
            if (name == "int")
                return Int32Type;
            if (_udts.TryGetValue(name, out Type type))
                return type;
            return null;
        }

        public Type CreateUDTStruct(string name, Type[] elementTypes, bool packed = false)
        {
            if (_udts.ContainsKey(name))
                throw new InvalidOperationException($"struct '{name}' is already defined");
            Type type = CAPI.LLVMStructCreateNamed(_context, name);
            CAPI.LLVMStructSetBody(type, Array.ConvertAll(elementTypes, tp => (CAPI.LLVMTypeRef) tp),
                (uint) elementTypes.Length, packed);
            _udts.Add(name, type);
            return type;
        }

        public BasicBlock CreateBasicBlock(string name = "") => CAPI.LLVMCreateBasicBlockInContext(_context, name);

        public BasicBlock AppendBasicBlock(Value fn, string name = "") =>
            CAPI.LLVMAppendBasicBlockInContext(_context, fn, name);

        public BasicBlock InsertBasicBlock(BasicBlock before, string name = "") =>
            CAPI.LLVMInsertBasicBlockInContext(_context, before, name);

        public Value MetadataAsValue(Metadata md) => CAPI.LLVMMetadataAsValue(_context, md);

        public struct DiagnosticInfo
        {
            public CAPI.LLVMDiagnosticSeverity Severity;
            public string DescriptionString;
        }

        public delegate void DiagnosticHandler(DiagnosticInfo diagnosticInfo);

        public void SetDiagnosticHandler(DiagnosticHandler handler)
        {
            if (_diagnosticHandlerHandle.IsAllocated)
                _diagnosticHandlerHandle.Free();

            CAPI.LLVMDiagnosticHandler internalHandler = (diagnosticInfo, diagnosticContext) =>
            {
                DiagnosticInfo info;
                info.Severity = CAPI.LLVMGetDiagInfoSeverity(diagnosticInfo);
                info.DescriptionString = CAPI.LLVMGetDiagInfoDescriptionString(diagnosticInfo);
                handler(info);
                if (info.Severity == CAPI.LLVMDiagnosticSeverity.Error)
                    throw new LLVMException(info.DescriptionString);
            };
            _diagnosticHandlerHandle = GCHandle.Alloc(internalHandler);

            CAPI.LLVMContextSetDiagnosticHandler(_context, internalHandler, IntPtr.Zero);

            // Also set this handler in the global context for APIs that do not use a specific context
            CAPI.LLVMContextSetDiagnosticHandler(CAPI.LLVMGetGlobalContext(), internalHandler, IntPtr.Zero);
        }


        public static void Main(string[] args)
        {
            using (Context context = new Context()) {
                using (Module module = context.CreateModule("MyModule", true)) {
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
    }
}