using System;
using System.Collections.Generic;

namespace MonC.LLVM
{
    public sealed class DIBuilder : IDisposable
    {
        private CAPI.DI.LLVMDIBuilderRef _builder;
        public Metadata Int32Type;

        internal DIBuilder(CAPI.LLVMModuleRef module)
        {
            _builder = CAPI.DI.LLVMCreateDIBuilder(module);
            Int32Type = CreateBasicType("int", 32, CAPI.DI.LLVMDWARFTypeEncoding.signed, CAPI.DI.LLVMDIFlags.Zero);
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_builder.IsValid) {
                CAPI.DI.LLVMDisposeDIBuilder(_builder);
                _builder = new CAPI.DI.LLVMDIBuilderRef();
            }
        }

        ~DIBuilder()
        {
            DoDispose();
        }

        public void BuilderFinalize()
        {
            CAPI.DI.LLVMDIBuilderFinalize(_builder);
        }

        public Metadata CreateCompileUnit(
            CAPI.DI.LLVMDWARFSourceLanguage lang, Metadata fileRef, string producer, bool isOptimized, string flags,
            uint runtimeVer, string splitName, CAPI.DI.LLVMDWARFEmissionKind kind, uint dwoId, bool splitDebugInlining,
            bool debugInfoForProfiling, string sysRoot, string sdk) => new Metadata(
            CAPI.DI.LLVMDIBuilderCreateCompileUnitPublic(_builder, lang, fileRef, producer, isOptimized, flags,
                runtimeVer, splitName, kind, dwoId, splitDebugInlining, debugInfoForProfiling, sysRoot, sdk));

        public Metadata CreateModule(Metadata parentScope, string name, string configMacros, string includePath,
            string apiNotesFile) => new Metadata(CAPI.DI.LLVMDIBuilderCreateModule(_builder, parentScope, name,
            configMacros, includePath, apiNotesFile));

        public Metadata CreateBasicType(string name, ulong sizeInBits, CAPI.DI.LLVMDWARFTypeEncoding encoding,
            CAPI.DI.LLVMDIFlags flags) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateBasicTypePublic(_builder, name, sizeInBits, encoding, flags));

        public Metadata CreateStructType(Metadata scope, string name, Metadata file, uint lineNumber, ulong sizeInBits,
            uint alignInBits, CAPI.DI.LLVMDIFlags flags, Metadata derivedFrom, Metadata[] elements, uint runTimeLang,
            Metadata vTableHolder, string uniqueId) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateStructTypePublic(_builder, scope, name, file, lineNumber,
                sizeInBits, alignInBits, flags, derivedFrom,
                Array.ConvertAll(elements, elem => (CAPI.LLVMMetadataRef) elem), runTimeLang, vTableHolder, uniqueId));

        public Metadata CreateSubroutineType(
            Metadata file, Metadata[] parameterTypes, CAPI.DI.LLVMDIFlags flags) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateSubroutineTypePublic(_builder, file,
                Array.ConvertAll(parameterTypes, pType => (CAPI.LLVMMetadataRef) pType), flags));

        public Metadata CreateReplaceableCompositeType(CAPI.DI.LLVMDWARFTag tag, string name, Metadata scope,
            Metadata file, uint line, uint runtimeLang,
            ulong sizeInBits, uint alignInBits, CAPI.DI.LLVMDIFlags flags, string uniqueIdentifier) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateReplaceableCompositeTypePublic(_builder, tag, name, scope, file,
                line, runtimeLang, sizeInBits, alignInBits, flags, uniqueIdentifier));

        public Metadata CreatePointerType(Metadata pointeeTy, ulong sizeInBits, uint alignInBits, uint addressSpace = 0,
            string name = "") =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreatePointerTypePublic(_builder, pointeeTy, sizeInBits, alignInBits,
                addressSpace, name));

        public Metadata CreatePointerType(Metadata pointeeTy) =>
            CreatePointerType(pointeeTy, pointeeTy.TypeSizeInBits, pointeeTy.TypeAlignInBits);

        public Metadata CreateFile(string filename, string directory) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateFilePublic(_builder, filename, directory));

        public Metadata CreateFunction(Metadata scope, string name, string linkageName, Metadata file, uint lineNo,
            Metadata ty, bool isLocalToUnit, bool isDefinition, uint scopeLine, CAPI.DI.LLVMDIFlags flags,
            bool isOptimized) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateFunctionPublic(_builder, scope, name, linkageName, file, lineNo, ty,
                isLocalToUnit, isDefinition, scopeLine, flags, isOptimized));

        public Metadata CreateEnumerator(string name, long value, bool isUnsigned) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateEnumeratorPublic(_builder, name, value, isUnsigned));

        public Metadata CreateEnumerationType(Metadata scope, string name, UIntPtr nameLen, Metadata file,
            uint lineNumber, ulong sizeInBits, uint alignInBits, Metadata[] elements, uint numElements,
            Metadata classTy) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateEnumerationTypePublic(_builder, scope, name, file,
                lineNumber, sizeInBits, alignInBits, Array.ConvertAll(elements, elem => (CAPI.LLVMMetadataRef) elem),
                classTy));

        public Metadata CreateExpression(long[] addr) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateExpressionPublic(_builder, addr));

        public Metadata CreateExpression() => CreateExpression(new long[] { });

        public Metadata CreateAutoVariable(Metadata scope, string name, Metadata file, uint lineNo, Metadata ty,
            bool alwaysPreserve, CAPI.DI.LLVMDIFlags flags, uint alignInBits) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateAutoVariablePublic(_builder, scope, name, file, lineNo, ty,
                alwaysPreserve, flags, alignInBits));

        public Metadata CreateParameterVariable(Metadata scope, string name, uint argNo, Metadata file, uint lineNo,
            Metadata ty, bool alwaysPreserve, CAPI.DI.LLVMDIFlags flags) =>
            new Metadata(CAPI.DI.LLVMDIBuilderCreateParameterVariablePublic(_builder, scope, name, argNo, file, lineNo,
                ty, alwaysPreserve, flags));

        public Value InsertDeclareAtEnd(Value storage, Metadata varInfo, Metadata expr, Metadata debugLoc,
            BasicBlock block) =>
            new Value(CAPI.DI.LLVMDIBuilderInsertDeclareAtEnd(_builder, storage, varInfo, expr, debugLoc, block));

        public Value InsertDbgValueAtEnd(Value val, Metadata varInfo, Metadata expr, Metadata debugLoc,
            BasicBlock block) =>
            new Value(CAPI.DI.LLVMDIBuilderInsertDbgValueAtEnd(_builder, val, varInfo, expr, debugLoc, block));


        private Dictionary<string, Metadata> _udts = new Dictionary<string, Metadata>();

        public Metadata? LookupType(string name)
        {
            if (name.Length == 0)
                return null;
            if (name == "int")
                return Int32Type;
            if (_udts.TryGetValue(name, out Metadata type))
                return type;
            return null;
        }

        public Metadata CreateUDTStruct(string name, Metadata file, uint lineNumber, ulong sizeInBits, uint alignInBits,
            Metadata[] elementTypes)
        {
            if (_udts.ContainsKey(name))
                throw new InvalidOperationException($"struct '{name}' is already defined");
            Metadata ret = CreateStructType(file, name, file, lineNumber, sizeInBits, alignInBits,
                CAPI.DI.LLVMDIFlags.Zero, Metadata.Null, elementTypes, 0, Metadata.Null, name);
            _udts.Add(name, ret);
            return ret;
        }
    }
}