using System;
using System.Collections.Generic;
using MonC.TypeSystem;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public enum LLVMDWARFTag : uint
    {
        subroutine_type = 0x0015
    }

    public enum LLVMDWARFTypeEncoding : uint
    {
        address = 0x01,
        boolean = 0x02,
        complex_float = 0x03,
        _float = 0x04,
        signed = 0x05,
        signed_char = 0x06,
        unsigned = 0x07,
        unsigned_char = 0x08,
        imaginary_float = 0x09,
        packed_decimal = 0x0a,
        numeric_string = 0x0b,
        edited = 0x0c,
        signed_fixed = 0x0d,
        unsigned_fixed = 0x0e,
        decimal_float = 0x0f,
        UTF = 0x10,
        UCS = 0x11,
        ASCII = 0x12
    }

    public sealed class DIBuilder : IDisposable
    {
        private LLVMDIBuilderRef _builder;
        public Metadata Int32Type;

        internal DIBuilder(LLVMModuleRef module)
        {
            _builder = module.CreateDIBuilder();
            Int32Type = CreateBasicType("int", 32, LLVMDWARFTypeEncoding.signed, LLVMDIFlags.LLVMDIFlagZero);
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private unsafe void DoDispose()
        {
            if (_builder.Handle != IntPtr.Zero) {
                LLVMSharp.Interop.LLVM.DisposeDIBuilder(_builder);
                _builder = new LLVMDIBuilderRef();
            }
        }

        ~DIBuilder() => DoDispose();

        public void BuilderFinalize() => _builder.DIBuilderFinalize();

        public Metadata CreateCompileUnit(LLVMDWARFSourceLanguage lang, Metadata fileRef, string producer,
            bool isOptimized, string flags, uint runtimeVer, string splitName, LLVMDWARFEmissionKind kind, uint dwoId,
            bool splitDebugInlining, bool debugInfoForProfiling) =>
            _builder.CreateCompileUnit(lang, fileRef, producer, isOptimized ? 1 : 0, flags, runtimeVer, splitName, kind,
                dwoId, splitDebugInlining ? 1 : 0, debugInfoForProfiling ? 1 : 0);

        public Metadata CreateModule(Metadata parentScope, string name, string configMacros, string includePath,
            string apiNotesFile) =>
            _builder.CreateModule(parentScope, name, configMacros, includePath, apiNotesFile);

        public unsafe Metadata CreateBasicType(string name, ulong sizeInBits, LLVMDWARFTypeEncoding encoding,
            LLVMDIFlags flags)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateBasicType(_builder, marshaledName,
                (UIntPtr) marshaledName.Length, sizeInBits, (uint) encoding, flags);
        }

        public unsafe Metadata CreateStructType(Metadata scope, string name, Metadata file, uint lineNumber,
            ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, Metadata derivedFrom, Metadata[] elements,
            uint runTimeLang, Metadata vTableHolder, string uniqueId)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            using var marshaledUniqueId = new MarshaledString(uniqueId.AsSpan());
            fixed (LLVMMetadataRef* castElems = Array.ConvertAll(elements, elem => (LLVMMetadataRef) elem).AsSpan()) {
                return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateStructType(_builder,
                    (LLVMMetadataRef) scope, marshaledName, (UIntPtr) marshaledName.Length, (LLVMMetadataRef) file,
                    lineNumber, sizeInBits, alignInBits, flags, (LLVMMetadataRef) derivedFrom,
                    (LLVMOpaqueMetadata**) castElems, (uint) elements.Length, runTimeLang,
                    (LLVMMetadataRef) vTableHolder, marshaledUniqueId, (UIntPtr) marshaledUniqueId.Length);
            }
        }

        public Metadata CreateSubroutineType(Metadata file, Metadata[] parameterTypes, LLVMDIFlags flags) =>
            _builder.CreateSubroutineType(file, Array.ConvertAll(parameterTypes, pType => (LLVMMetadataRef) pType),
                flags);

        public unsafe Metadata CreateReplaceableCompositeType(LLVMDWARFTag tag, string name, Metadata scope,
            Metadata file, uint line, uint runtimeLang, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags,
            string uniqueIdentifier)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            using var marshaledUniqueId = new MarshaledString(uniqueIdentifier.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateReplaceableCompositeType(_builder,
                (uint) tag, marshaledName, (UIntPtr) name.Length, (LLVMMetadataRef) scope, (LLVMMetadataRef) file, line,
                runtimeLang, sizeInBits, alignInBits, flags, marshaledUniqueId, (UIntPtr) uniqueIdentifier.Length);
        }

        public unsafe Metadata CreatePointerType(Metadata pointeeTy, ulong sizeInBits, uint alignInBits,
            uint addressSpace = 0, string name = "")
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreatePointerType(_builder,
                (LLVMMetadataRef) pointeeTy, sizeInBits, alignInBits, addressSpace, marshaledName,
                (UIntPtr) name.Length);
        }

        public Metadata CreatePointerType(Metadata pointeeTy) =>
            CreatePointerType(pointeeTy, pointeeTy.GetTypeSizeInBits(), pointeeTy.GetTypeAlignInBits());

        public Metadata CreateFile(string filename, string directory) =>
            _builder.CreateFile(filename, directory);

        public Metadata CreateFunction(Metadata scope, string name, string linkageName, Metadata file, uint lineNo,
            Metadata ty, bool isLocalToUnit, bool isDefinition, uint scopeLine, LLVMDIFlags flags, bool isOptimized) =>
            _builder.CreateFunction(scope, name, linkageName, file, lineNo, ty, isLocalToUnit ? 1 : 0,
                isDefinition ? 1 : 0, scopeLine, flags, isOptimized ? 1 : 0);

        public unsafe Metadata CreateLexicalBlock(Metadata scope, Metadata file, uint line, uint column) =>
            (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateLexicalBlock(_builder, (LLVMMetadataRef) scope,
                (LLVMMetadataRef) file, line, column);

        public unsafe Metadata CreateEnumerator(string name, long value, bool isUnsigned)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateEnumerator(_builder, marshaledName,
                (UIntPtr) name.Length, value, isUnsigned ? 1 : 0);
        }

        public unsafe Metadata CreateEnumerationType(Metadata scope, string name, Metadata file, uint lineNumber,
            ulong sizeInBits, uint alignInBits, Metadata[] elements, Metadata classTy)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            fixed (LLVMMetadataRef* castElems = Array.ConvertAll(elements, elem => (LLVMMetadataRef) elem).AsSpan()) {
                return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateEnumerationType(_builder,
                    (LLVMMetadataRef) scope, marshaledName, (UIntPtr) name.Length, (LLVMMetadataRef) file, lineNumber,
                    sizeInBits, alignInBits, (LLVMOpaqueMetadata**) castElems, (uint) elements.Length,
                    (LLVMMetadataRef) classTy);
            }
        }

        public unsafe Metadata CreateExpression(long[] addr)
        {
            fixed (long* castAddr = addr.AsSpan()) {
                return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateExpression(_builder, castAddr,
                    (UIntPtr) addr.Length);
            }
        }

        public Metadata CreateExpression() => CreateExpression(new long[] { });

        public unsafe Metadata CreateAutoVariable(Metadata scope, string name, Metadata file, uint lineNo, Metadata ty,
            bool alwaysPreserve, LLVMDIFlags flags, uint alignInBits)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateAutoVariable(_builder,
                (LLVMMetadataRef) scope, marshaledName, (UIntPtr) name.Length, (LLVMMetadataRef) file, lineNo,
                (LLVMMetadataRef) ty, alwaysPreserve ? 1 : 0, flags, alignInBits);
        }


        public unsafe Metadata CreateParameterVariable(Metadata scope, string name, uint argNo, Metadata file,
            uint lineNo, Metadata ty, bool alwaysPreserve, LLVMDIFlags flags)
        {
            using var marshaledName = new MarshaledString(name.AsSpan());
            return (LLVMMetadataRef) LLVMSharp.Interop.LLVM.DIBuilderCreateParameterVariable(_builder,
                (LLVMMetadataRef) scope, marshaledName, (UIntPtr) name.Length, argNo, (LLVMMetadataRef) file, lineNo,
                (LLVMMetadataRef) ty, alwaysPreserve ? 1 : 0, flags);
        }


        public unsafe Value InsertDeclareAtEnd(Value storage, Metadata varInfo, Metadata expr, Metadata debugLoc,
            BasicBlock block) =>
            (LLVMValueRef) LLVMSharp.Interop.LLVM.DIBuilderInsertDeclareAtEnd(_builder, (LLVMValueRef) storage,
                (LLVMMetadataRef) varInfo, (LLVMMetadataRef) expr, (LLVMMetadataRef) debugLoc,
                (LLVMBasicBlockRef) block);

        public unsafe Value InsertDbgValueAtEnd(Value val, Metadata varInfo, Metadata expr, Metadata debugLoc,
            BasicBlock block) =>
            (LLVMValueRef) LLVMSharp.Interop.LLVM.DIBuilderInsertDbgValueAtEnd(_builder, (LLVMValueRef) val,
                (LLVMMetadataRef) varInfo, (LLVMMetadataRef) expr, (LLVMMetadataRef) debugLoc,
                (LLVMBasicBlockRef) block);


        private Dictionary<string, Metadata> _structs = new Dictionary<string, Metadata>();

        public Metadata? LookupPrimitiveType(Primitive primitive)
        {
            return primitive switch {
                Primitive.Void => null,
                Primitive.Int => Int32Type,
                _ => null
            };
        }

        public Metadata? LookupStructType(string name)
        {
            if (_structs.TryGetValue(name, out Metadata type))
                return type;
            return null;
        }

        public Metadata CreateStruct(string name, Metadata file, uint lineNumber, ulong sizeInBits, uint alignInBits,
            Metadata[] elementTypes)
        {
            if (_structs.ContainsKey(name))
                throw new InvalidOperationException($"struct '{name}' is already defined");
            Metadata ret = CreateStructType(file, name, file, lineNumber, sizeInBits, alignInBits,
                LLVMDIFlags.LLVMDIFlagZero, Metadata.Null, elementTypes, 0, Metadata.Null, name);
            _structs.Add(name, ret);
            return ret;
        }
    }
}
