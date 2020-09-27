using System;
using System.Runtime.InteropServices;

namespace MonC.LLVM
{
    public static class CAPI
    {
        // Suppress warnings about InternalPtr fields not being assigned to
#pragma warning disable 649

        public struct LLVMContextRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMContextRef LLVMGetGlobalContext();

        [DllImport("LLVM-C")]
        public static extern LLVMContextRef LLVMContextCreate();

        [DllImport("LLVM-C")]
        public static extern void LLVMContextDispose(LLVMContextRef context);


        public struct LLVMDiagnosticInfoRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        public delegate void LLVMDiagnosticHandler(LLVMDiagnosticInfoRef diagnosticInfo, IntPtr diagnosticContext);

        [DllImport("LLVM-C")]
        public static extern void LLVMContextSetDiagnosticHandler(LLVMContextRef context,
            LLVMDiagnosticHandler handler,
            IntPtr diagnosticContext);

        public enum LLVMDiagnosticSeverity
        {
            Error,
            Warning,
            Remark,
            Note
        }

        [DllImport("LLVM-C")]
        public static extern LLVMDiagnosticSeverity LLVMGetDiagInfoSeverity(LLVMDiagnosticInfoRef diagnosticInfo);

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetDiagInfoDescription(LLVMDiagnosticInfoRef diagnosticInfo);

        [DllImport("LLVM-C")]
        private static extern void LLVMDisposeMessage(IntPtr message);

        private static string MarshallMessage(IntPtr strPtr)
        {
            string? str = Marshal.PtrToStringAnsi(strPtr);
            LLVMDisposeMessage(strPtr);
            return str ?? throw new NullReferenceException("null string marshalled from LLVM");
        }

        public static string LLVMGetDiagInfoDescriptionString(LLVMDiagnosticInfoRef diagnosticInfo) =>
            MarshallMessage(LLVMGetDiagInfoDescription(diagnosticInfo));


        public struct LLVMTypeRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
            public static bool operator ==(LLVMTypeRef a, LLVMTypeRef b) => a.InternalPtr == b.InternalPtr;
            public static bool operator !=(LLVMTypeRef a, LLVMTypeRef b) => a.InternalPtr != b.InternalPtr;

            public override bool Equals(Object obj)
            {
                if (GetType() != obj.GetType())
                    return false;
                return this == (LLVMTypeRef) obj;
            }

            public override int GetHashCode() => InternalPtr.GetHashCode();
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMVoidTypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt1TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt8TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt16TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt32TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt64TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMInt128TypeInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMIntTypeInContext(LLVMContextRef context, uint numBits);

        [DllImport("LLVM-C")]
        public static extern uint LLVMGetIntTypeWidth(LLVMTypeRef integerTy);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMFunctionType(LLVMTypeRef returnType, LLVMTypeRef[] paramTypes,
            uint paramCount, bool isVarArg);

        [DllImport("LLVM-C")]
        public static extern bool LLVMIsFunctionVarArg(LLVMTypeRef functionTy);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMGetReturnType(LLVMTypeRef functionTy);

        [DllImport("LLVM-C")]
        public static extern uint LLVMCountParamTypes(LLVMTypeRef functionTy);

        [DllImport("LLVM-C")]
        private static extern void LLVMGetParamTypes(LLVMTypeRef functionTy, IntPtr destOutBuf);

        public static LLVMTypeRef[] LLVMGetParamTypes(LLVMTypeRef functionTy)
        {
            int elemSize = Marshal.SizeOf<LLVMTypeRef>();
            int numParams = (int) LLVMCountParamTypes(functionTy);
            IntPtr arrayBuf = Marshal.AllocHGlobal(elemSize * numParams);
            LLVMGetParamTypes(functionTy, arrayBuf);
            LLVMTypeRef[] paramsOut = new LLVMTypeRef[numParams];
            for (int i = 0; i < numParams; ++i)
                paramsOut[i] = Marshal.PtrToStructure<LLVMTypeRef>(arrayBuf + elemSize * i);
            Marshal.FreeHGlobal(arrayBuf);
            return paramsOut;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMStructCreateNamed(LLVMContextRef context, string name);

        [DllImport("LLVM-C")]
        public static extern void LLVMStructSetBody(LLVMTypeRef structTy, LLVMTypeRef[] elementTypes,
            uint elementCount, bool packed);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMArrayType(LLVMTypeRef elementType, uint elementCount);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMPointerType(LLVMTypeRef elementType, uint addressSpace);

        public enum LLVMTypeKind
        {
            Void,
            Half,
            Float,
            Double,
            X86_FP80,
            FP128,
            PPC_FP128,
            Label,
            Integer,
            Function,
            Struct,
            Array,
            Pointer,
            Vector,
            Metadata,
            X86_MMX,
            Token,
            ScalableVector,
            BFloat
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTypeKind LLVMGetTypeKind(LLVMTypeRef ty);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMGetElementType(LLVMTypeRef ty);

        [DllImport("LLVM-C")]
        public static extern uint LLVMGetVectorSize(LLVMTypeRef vectorTy);

        [DllImport("LLVM-C")]
        public static extern uint LLVMGetPointerAddressSpace(LLVMTypeRef pointerTy);


        public struct LLVMValueRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMConstInt(LLVMTypeRef intTy, ulong n, bool signExtend);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMConstReal(LLVMTypeRef realTy, double n);


        public struct LLVMModuleRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMModuleRef LLVMModuleCreateWithNameInContext(string moduleId, LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeModule(LLVMModuleRef module);

        public enum LLVMModuleFlagBehavior
        {
            Error,
            Warning,
            Require,
            Override,
            Append,
            AppendUnique,
        }

        [DllImport("LLVM-C")]
        private static extern void LLVMAddModuleFlag(LLVMModuleRef module, LLVMModuleFlagBehavior behavior, string key,
            UIntPtr keyLen, LLVMMetadataRef val);

        public static void LLVMAddModuleFlag(LLVMModuleRef module, LLVMModuleFlagBehavior behavior, string key,
            LLVMMetadataRef val) => LLVMAddModuleFlag(module, behavior, key, (UIntPtr) key.Length, val);

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTarget(LLVMModuleRef m);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetTarget(LLVMModuleRef m, string triple);

        [DllImport("LLVM-C")]
        public static extern void LLVMDumpModule(LLVMModuleRef module);

        [DllImport("LLVM-C")]
        private static extern bool
            LLVMPrintModuleToFile(LLVMModuleRef module, string filename, out IntPtr errorMessage);

        public static bool LLVMPrintModuleToFile(LLVMModuleRef module, string filename, out string? errorMessage)
        {
            bool ret = LLVMPrintModuleToFile(module, filename, out IntPtr msgPtr);
            errorMessage = MarshallMessage(msgPtr);
            return ret;
        }

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMPrintModuleToString(LLVMModuleRef module);

        public static string LLVMPrintModuleToStringPublic(LLVMModuleRef module) =>
            MarshallMessage(LLVMPrintModuleToString(module));

        [DllImport("LLVM-C")]
        public static extern int LLVMWriteBitcodeToFile(LLVMModuleRef m, string path);

        [DllImport("LLVM-C")]
        public static extern LLVMMemoryBufferRef LLVMWriteBitcodeToMemoryBuffer(LLVMModuleRef m);

        [DllImport("LLVM-C")]
        private static extern bool LLVMParseIRInContext(LLVMContextRef context, LLVMMemoryBufferRef memBuf,
            out LLVMModuleRef moduleOut, out IntPtr outMessage);

        public static bool LLVMParseIRInContext(LLVMContextRef context, LLVMMemoryBufferRef memBuf,
            out LLVMModuleRef moduleOut, out string? outMessage)
        {
            bool ret = LLVMParseIRInContext(context, memBuf, out moduleOut, out IntPtr msgPtr);
            outMessage = MarshallMessage(msgPtr);
            return ret;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMAddFunction(LLVMModuleRef module, string name, LLVMTypeRef functionTy);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetSubprogram(LLVMValueRef func, LLVMMetadataRef sp);

        [DllImport("LLVM-C")]
        public static extern bool LLVMIsUndef(LLVMValueRef val);

        [DllImport("LLVM-C")]
        public static extern bool LLVMIsDeclaration(LLVMValueRef global);

        [DllImport("LLVM-C")]
        public static extern uint LLVMCountParams(LLVMValueRef fn);

        [DllImport("LLVM-C")]
        private static extern void LLVMGetParams(LLVMValueRef fn, IntPtr paramsOutBuf);

        public static LLVMValueRef[] LLVMGetParams(LLVMValueRef fn)
        {
            int elemSize = Marshal.SizeOf<LLVMValueRef>();
            int numParams = (int) LLVMCountParams(fn);
            IntPtr arrayBuf = Marshal.AllocHGlobal(elemSize * numParams);
            LLVMGetParams(fn, arrayBuf);
            LLVMValueRef[] paramsOut = new LLVMValueRef[numParams];
            for (int i = 0; i < numParams; ++i)
                paramsOut[i] = Marshal.PtrToStructure<LLVMValueRef>(arrayBuf + elemSize * i);
            Marshal.FreeHGlobal(arrayBuf);
            return paramsOut;
        }

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetValueName2(LLVMValueRef val, out UIntPtr lengthOut);

        public static string LLVMGetValueName2(LLVMValueRef val)
        {
            IntPtr strPtr = LLVMGetValueName2(val, out UIntPtr length);
            return Marshal.PtrToStringAnsi(strPtr, (int) length);
        }

        [DllImport("LLVM-C")]
        private static extern void LLVMSetValueName2(LLVMValueRef val, string name, UIntPtr nameLen);

        public static void LLVMSetValueName2(LLVMValueRef val, string name) =>
            LLVMSetValueName2(val, name, (UIntPtr) name.Length);


        public enum LLVMLinkage
        {
            External,
            AvailableExternally,
            LinkOnceAny,
            LinkOnceODR,
            LinkOnceODRAutoHide,
            WeakAny,
            WeakODR,
            Appending,
            Internal,
            Private,
            DLLImport,
            DLLExport,
            ExternalWeak,
            Ghost,
            Common,
            LinkerPrivate,
            LinkerPrivateWeak
        }

        [DllImport("LLVM-C")]
        public static extern LLVMLinkage LLVMGetLinkage(LLVMValueRef global);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetLinkage(LLVMValueRef global, LLVMLinkage linkage);

        public enum LLVMValueKind
        {
            Argument,
            BasicBlock,
            MemoryUse,
            MemoryDef,
            MemoryPhi,

            Function,
            GlobalAlias,
            GlobalIFunc,
            GlobalVariable,
            BlockAddress,
            ConstantExpr,
            ConstantArray,
            ConstantStruct,
            ConstantVector,

            UndefValue,
            ConstantAggregateZero,
            ConstantDataArray,
            ConstantDataVector,
            ConstantInt,
            ConstantFP,
            ConstantPointerNull,
            ConstantTokenNone,

            MetadataAsValue,
            InlineAsm,

            Instruction,

            Invalid
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueKind LLVMGetValueKind(LLVMValueRef val);

        [DllImport("LLVM-C")]
        public static extern LLVMTypeRef LLVMTypeOf(LLVMValueRef val);


        public struct LLVMBasicBlockRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMCreateBasicBlockInContext(LLVMContextRef context, string name = "");

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMAppendBasicBlockInContext(LLVMContextRef context, LLVMValueRef fn,
            string name = "");

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMInsertBasicBlockInContext(LLVMContextRef context,
            LLVMBasicBlockRef bb, string name = "");

        [DllImport("LLVM-C")]
        public static extern void LLVMInsertExistingBasicBlockAfterInsertBlock(LLVMBuilderRef builder,
            LLVMBasicBlockRef bb);

        [DllImport("LLVM-C")]
        public static extern void LLVMAppendExistingBasicBlock(LLVMValueRef fn, LLVMBasicBlockRef bb);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetBasicBlockParent(LLVMBasicBlockRef basicBlock);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetBasicBlockTerminator(LLVMBasicBlockRef basicBlock);

        [DllImport("LLVM-C")]
        public static extern uint LLVMCountBasicBlocks(LLVMValueRef fn);

        [DllImport("LLVM-C")]
        private static extern void LLVMGetBasicBlocks(LLVMValueRef fn, IntPtr basicBlocksOutBuf);

        public static LLVMBasicBlockRef[] LLVMGetBasicBlocks(LLVMValueRef fn)
        {
            int elemSize = Marshal.SizeOf<LLVMBasicBlockRef>();
            int numBasicBlocks = (int) LLVMCountBasicBlocks(fn);
            IntPtr arrayBuf = Marshal.AllocHGlobal(elemSize * numBasicBlocks);
            LLVMGetBasicBlocks(fn, arrayBuf);
            LLVMBasicBlockRef[] basicBlocks = new LLVMBasicBlockRef[numBasicBlocks];
            for (int i = 0; i < numBasicBlocks; ++i)
                basicBlocks[i] = Marshal.PtrToStructure<LLVMBasicBlockRef>(arrayBuf + elemSize * i);
            Marshal.FreeHGlobal(arrayBuf);
            return basicBlocks;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetFirstBasicBlock(LLVMValueRef fn);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetLastBasicBlock(LLVMValueRef fn);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetNextBasicBlock(LLVMBasicBlockRef basicBlock);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetPreviousBasicBlock(LLVMBasicBlockRef basicBlock);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetEntryBasicBlock(LLVMValueRef fn);


        public struct LLVMBuilderRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMBuilderRef LLVMCreateBuilderInContext(LLVMContextRef context);

        [DllImport("LLVM-C")]
        public static extern void LLVMPositionBuilder(LLVMBuilderRef builder, LLVMBasicBlockRef block,
            LLVMValueRef instr);

        [DllImport("LLVM-C")]
        public static extern void LLVMPositionBuilderBefore(LLVMBuilderRef builder, LLVMValueRef instr);

        [DllImport("LLVM-C")]
        public static extern void LLVMPositionBuilderAtEnd(LLVMBuilderRef builder, LLVMBasicBlockRef block);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetInsertBlock(LLVMBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern void LLVMClearInsertionPosition(LLVMBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern void LLVMInsertIntoBuilder(LLVMBuilderRef builder, LLVMValueRef instr);

        [DllImport("LLVM-C")]
        public static extern void LLVMInsertIntoBuilderWithName(LLVMBuilderRef builder, LLVMValueRef instr,
            string name);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeBuilder(LLVMBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildRetVoid(LLVMBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildRet(LLVMBuilderRef builder, LLVMValueRef value);

        [DllImport("LLVM-C")]
        private static extern LLVMValueRef
            LLVMBuildAggregateRet(LLVMBuilderRef builder, LLVMValueRef[] retVals, uint n);

        public static LLVMValueRef LLVMBuildAggregateRet(LLVMBuilderRef builder, LLVMValueRef[] retVals) =>
            LLVMBuildAggregateRet(builder, retVals, (uint) retVals.Length);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildBr(LLVMBuilderRef builder, LLVMBasicBlockRef dest);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildCondBr(LLVMBuilderRef builder, LLVMValueRef ifVal,
            LLVMBasicBlockRef thenBlock, LLVMBasicBlockRef elseBlock);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildSwitch(LLVMBuilderRef builder, LLVMValueRef v,
            LLVMBasicBlockRef elseBlock, uint numCases);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildIndirectBr(LLVMBuilderRef builder, LLVMValueRef addr, uint numDests);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildAdd(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNSWAdd(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNUWAdd(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFAdd(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildSub(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNSWSub(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNUWSub(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFSub(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildMul(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNSWMul(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNUWMul(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFMul(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildUDiv(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildExactUDiv(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildSDiv(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildExactSDiv(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFDiv(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildURem(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildSRem(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFRem(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildShl(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildLShr(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildAShr(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildAnd(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildOr(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildXor(LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildBinOp(LLVMBuilderRef builder, LLVMOpcode op, LLVMValueRef lhs,
            LLVMValueRef rhs, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNeg(LLVMBuilderRef builder, LLVMValueRef v, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNSWNeg(LLVMBuilderRef builder, LLVMValueRef v, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNUWNeg(LLVMBuilderRef builder, LLVMValueRef v, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFNeg(LLVMBuilderRef builder, LLVMValueRef v, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildNot(LLVMBuilderRef builder, LLVMValueRef v, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildCast(LLVMBuilderRef builder, LLVMOpcode op, LLVMValueRef val,
            LLVMTypeRef destTy, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildPointerCast(LLVMBuilderRef builder, LLVMValueRef val,
            LLVMTypeRef destTy, string name);

        public enum LLVMIntPredicate
        {
            IntEQ = 32,
            IntNE,
            IntUGT,
            IntUGE,
            IntULT,
            IntULE,
            IntSGT,
            IntSGE,
            IntSLT,
            IntSLE
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildICmp(LLVMBuilderRef builder, LLVMIntPredicate op, LLVMValueRef lhs,
            LLVMValueRef rhs, string name);

        public enum LLVMRealPredicate
        {
            RealPredicateFalse,
            RealOEQ,
            RealOGT,
            RealOGE,
            RealOLT,
            RealOLE,
            RealONE,
            RealORD,
            RealUNO,
            RealUEQ,
            RealUGT,
            RealUGE,
            RealULT,
            RealULE,
            RealUNE,
            RealPredicateTrue
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildFCmp(LLVMBuilderRef builder, LLVMRealPredicate op, LLVMValueRef lhs,
            LLVMValueRef rhs, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildPhi(LLVMBuilderRef builder, LLVMTypeRef ty, string name);

        [DllImport("LLVM-C")]
        private static extern LLVMValueRef LLVMBuildCall2(LLVMBuilderRef builder, LLVMTypeRef ty, LLVMValueRef fn,
            LLVMValueRef[] args, uint numArgs, string name);

        public static LLVMValueRef LLVMBuildCall2(LLVMBuilderRef builder, LLVMTypeRef ty, LLVMValueRef fn,
            LLVMValueRef[] args, string name) => LLVMBuildCall2(builder, ty, fn, args, (uint) args.Length, name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildSelect(LLVMBuilderRef builder, LLVMValueRef _if, LLVMValueRef then,
            LLVMValueRef _else, string name);

        [DllImport("LLVM-C")]
        private static extern void LLVMAddIncoming(LLVMValueRef phiNode, LLVMValueRef[] incomingValues,
            LLVMBasicBlockRef[] incomingBlocks, uint count);

        public static void LLVMAddIncoming(LLVMValueRef phiNode, LLVMValueRef[] incomingValues,
            LLVMBasicBlockRef[] incomingBlocks)
        {
            if (incomingValues.Length != incomingBlocks.Length)
                throw new InvalidOperationException("values and blocks array must be the same length");
            LLVMAddIncoming(phiNode, incomingValues, incomingBlocks, (uint) incomingValues.Length);
        }

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildAlloca(LLVMBuilderRef builder, LLVMTypeRef ty, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildArrayAlloca(LLVMBuilderRef builder, LLVMTypeRef ty, LLVMValueRef val,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildLoad2(LLVMBuilderRef builder, LLVMTypeRef ty, LLVMValueRef ptr,
            string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildStore(LLVMBuilderRef builder, LLVMValueRef val, LLVMValueRef ptr);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildGlobalString(LLVMBuilderRef builder, string str, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMBuildGlobalStringPtr(LLVMBuilderRef builder, string str, string name);

        [DllImport("LLVM-C")]
        public static extern LLVMBasicBlockRef LLVMGetInstructionParent(LLVMValueRef inst);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetNextInstruction(LLVMValueRef inst);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetPreviousInstruction(LLVMValueRef inst);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetFirstInstruction(LLVMBasicBlockRef basicBlock);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMGetLastInstruction(LLVMBasicBlockRef basicBlock);

        public enum LLVMOpcode
        {
            Invalid = 0,

            /* Terminator Instructions */
            Ret = 1,
            Br = 2,
            Switch = 3,
            IndirectBr = 4,
            Invoke = 5,

            /* removed 6 due to API changes */
            Unreachable = 7,
            CallBr = 67,

            /* Standard Unary Operators */
            FNeg = 66,

            /* Standard Binary Operators */
            Add = 8,
            FAdd = 9,
            Sub = 10,
            FSub = 11,
            Mul = 12,
            FMul = 13,
            UDiv = 14,
            SDiv = 15,
            FDiv = 16,
            URem = 17,
            SRem = 18,
            FRem = 19,

            /* Logical Operators */
            Shl = 20,
            LShr = 21,
            AShr = 22,
            And = 23,
            Or = 24,
            Xor = 25,

            /* Memory Operators */
            Alloca = 26,
            Load = 27,
            Store = 28,
            GetElementPtr = 29,

            /* Cast Operators */
            Trunc = 30,
            ZExt = 31,
            SExt = 32,
            FPToUI = 33,
            FPToSI = 34,
            UIToFP = 35,
            SIToFP = 36,
            FPTrunc = 37,
            FPExt = 38,
            PtrToInt = 39,
            IntToPtr = 40,
            BitCast = 41,
            AddrSpaceCast = 60,

            /* Other Operators */
            ICmp = 42,
            FCmp = 43,
            PHI = 44,
            Call = 45,
            Select = 46,
            UserOp1 = 47,
            UserOp2 = 48,
            VAArg = 49,
            ExtractElement = 50,
            InsertElement = 51,
            ShuffleVector = 52,
            ExtractValue = 53,
            InsertValue = 54,
            Freeze = 68,

            /* Atomic operators */
            Fence = 55,
            AtomicCmpXchg = 56,
            AtomicRMW = 57,

            /* Exception Handling Operators */
            Resume = 58,
            LandingPad = 59,
            CleanupRet = 61,
            CatchRet = 62,
            CatchPad = 63,
            CleanupPad = 64,
            CatchSwitch = 65
        }

        [DllImport("LLVM-C")]
        public static extern LLVMOpcode LLVMGetInstructionOpcode(LLVMValueRef inst);


        public struct LLVMMemoryBufferRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        private static extern bool LLVMCreateMemoryBufferWithContentsOfFile(string path,
            out LLVMMemoryBufferRef outMemBuf, out IntPtr outMessage);

        public static bool LLVMCreateMemoryBufferWithContentsOfFile(string path,
            out LLVMMemoryBufferRef outMemBuf, out string? outMessage)
        {
            bool ret = LLVMCreateMemoryBufferWithContentsOfFile(path, out outMemBuf, out IntPtr msgPtr);
            outMessage = MarshallMessage(msgPtr);
            return ret;
        }

        [DllImport("LLVM-C")]
        private static extern LLVMMemoryBufferRef LLVMCreateMemoryBufferWithMemoryRangeCopy(IntPtr inputData,
            UIntPtr inputDataLength, string bufferName);

        public static LLVMMemoryBufferRef LLVMCreateMemoryBufferWithMemoryRangeCopy(byte[] inputData, string bufferName)
        {
            IntPtr ptr = Marshal.AllocHGlobal(inputData.Length);
            Marshal.Copy(inputData, 0, ptr, inputData.Length);
            LLVMMemoryBufferRef ret =
                LLVMCreateMemoryBufferWithMemoryRangeCopy(ptr, (UIntPtr) inputData.Length, bufferName);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetBufferStart(LLVMMemoryBufferRef memBuf);

        public static byte[] LLVMGetBufferStartBytes(LLVMMemoryBufferRef memBuf)
        {
            IntPtr ptr = LLVMGetBufferStart(memBuf);
            UIntPtr size = LLVMGetBufferSize(memBuf);
            byte[] ret = new byte[(uint) size];
            Marshal.Copy(ptr, ret, 0, (int) size);
            return ret;
        }

        public static string LLVMGetBufferString(LLVMMemoryBufferRef memBuf)
        {
            IntPtr ptr = LLVMGetBufferStart(memBuf);
            UIntPtr size = LLVMGetBufferSize(memBuf);
            return Marshal.PtrToStringAnsi(ptr, (int) size);
        }

        [DllImport("LLVM-C")]
        public static extern UIntPtr LLVMGetBufferSize(LLVMMemoryBufferRef memBuf);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeMemoryBuffer(LLVMMemoryBufferRef memBuf);

        [DllImport("LLVM-C")]
        public static extern bool LLVMGetBitcodeModule2(LLVMMemoryBufferRef memBuf, out LLVMModuleRef outM);


        public struct LLVMMetadataRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMMDStringInContext2(LLVMContextRef c, string str, UIntPtr slen);

        public static LLVMMetadataRef LLVMMDStringInContext2(LLVMContextRef c, string str) =>
            LLVMMDStringInContext2(c, str, (UIntPtr) str.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMMDNodeInContext2(LLVMContextRef c, LLVMMetadataRef[] mds,
            UIntPtr count);

        public static LLVMMetadataRef LLVMMDNodeInContext2(LLVMContextRef c, LLVMMetadataRef[] mds) =>
            LLVMMDNodeInContext2(c, mds, (UIntPtr) mds.Length);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMMetadataAsValue(LLVMContextRef c, LLVMMetadataRef md);

        [DllImport("LLVM-C")]
        public static extern LLVMMetadataRef LLVMValueAsMetadata(LLVMValueRef val);

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetMDString(LLVMValueRef v, out uint length);

        public static string LLVMGetMDString(LLVMValueRef v)
        {
            IntPtr strPtr = LLVMGetMDString(v, out uint length);
            return Marshal.PtrToStringAnsi(strPtr, (int) length);
        }

        [DllImport("LLVM-C")]
        public static extern uint LLVMGetMDNodeNumOperands(LLVMValueRef v);

        [DllImport("LLVM-C")]
        private static extern void LLVMGetMDNodeOperands(LLVMValueRef v, IntPtr valueOutBuf);

        public static LLVMValueRef[] LLVMGetMDNodeOperands(LLVMValueRef v)
        {
            int elemSize = Marshal.SizeOf<LLVMValueRef>();
            int numOperands = (int) LLVMGetMDNodeNumOperands(v);
            IntPtr arrayBuf = Marshal.AllocHGlobal(elemSize * numOperands);
            LLVMGetMDNodeOperands(v, arrayBuf);
            LLVMValueRef[] operands = new LLVMValueRef[numOperands];
            for (int i = 0; i < numOperands; ++i)
                operands[i] = Marshal.PtrToStructure<LLVMValueRef>(arrayBuf + elemSize * i);
            Marshal.FreeHGlobal(arrayBuf);
            return operands;
        }

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeTemporaryMDNode(LLVMMetadataRef tempNode);

        [DllImport("LLVM-C")]
        public static extern void LLVMMetadataReplaceAllUsesWith(LLVMMetadataRef tempTargetMetadata,
            LLVMMetadataRef replacement);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetCurrentDebugLocation2(LLVMBuilderRef builder, LLVMMetadataRef loc);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetMetadata(LLVMValueRef val, uint kindId, LLVMValueRef node);


        public struct LLVMDIBuilderRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMDIBuilderRef LLVMCreateDIBuilder(LLVMModuleRef m);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeDIBuilder(LLVMDIBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern void LLVMDIBuilderFinalize(LLVMDIBuilderRef builder);

        [DllImport("LLVM-C")]
        public static extern uint LLVMDebugMetadataVersion();

        public enum LLVMDWARFSourceLanguage
        {
            C89,
            C,
            Ada83,
            C_plus_plus,
            Cobol74,
            Cobol85,
            Fortran77,
            Fortran90,
            Pascal83,
            Modula2,

            // New in DWARF v3:
            Java,
            C99,
            Ada95,
            Fortran95,
            PLI,
            ObjC,
            ObjC_plus_plus,
            UPC,
            D,

            // New in DWARF v4:
            Python,

            // New in DWARF v5:
            OpenCL,
            Go,
            Modula3,
            Haskell,
            C_plus_plus_03,
            C_plus_plus_11,
            OCaml,
            Rust,
            C11,
            Swift,
            Julia,
            Dylan,
            C_plus_plus_14,
            Fortran03,
            Fortran08,
            RenderScript,
            BLISS,

            // Vendor extensions:
            Mips_Assembler,
            GOOGLE_RenderScript,
            BORLAND_Delphi
        }

        public enum LLVMDWARFEmissionKind
        {
            None = 0,
            Full,
            LineTablesOnly
        }

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateCompileUnit(LLVMDIBuilderRef builder,
            LLVMDWARFSourceLanguage lang, LLVMMetadataRef fileRef, string producer, UIntPtr producerLen,
            bool isOptimized, string flags, UIntPtr flagsLen, uint runtimeVer, string splitName,
            UIntPtr splitNameLen, LLVMDWARFEmissionKind kind, uint dwoId, bool splitDebugInlining,
            bool debugInfoForProfiling, string sysRoot, UIntPtr sysRootLen, string sdk, UIntPtr sdkLen);

        public static LLVMMetadataRef LLVMDIBuilderCreateCompileUnit(LLVMDIBuilderRef builder,
            LLVMDWARFSourceLanguage lang, LLVMMetadataRef fileRef, string producer, bool isOptimized, string flags,
            uint runtimeVer, string splitName, LLVMDWARFEmissionKind kind, uint dwoId, bool splitDebugInlining,
            bool debugInfoForProfiling, string sysRoot, string sdk) =>
            LLVMDIBuilderCreateCompileUnit(builder, lang, fileRef, producer, (UIntPtr) producer.Length,
                isOptimized, flags, (UIntPtr) flags.Length, runtimeVer, splitName, (UIntPtr) splitName.Length, kind,
                dwoId, splitDebugInlining, debugInfoForProfiling, sysRoot, (UIntPtr) sysRoot.Length, sdk,
                (UIntPtr) sdk.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateModule(LLVMDIBuilderRef builder,
            LLVMMetadataRef parentScope, string name, UIntPtr nameLen, string configMacros, UIntPtr configMacrosLen,
            string includePath, UIntPtr includePathLen, string apiNotesFile, UIntPtr apiNotesFileLen);

        public static LLVMMetadataRef LLVMDIBuilderCreateModule(LLVMDIBuilderRef builder,
            LLVMMetadataRef parentScope, string name, string configMacros, string includePath,
            string apiNotesFile) =>
            LLVMDIBuilderCreateModule(builder, parentScope, name, (UIntPtr) name.Length, configMacros,
                (UIntPtr) configMacros.Length, includePath, (UIntPtr) includePath.Length, apiNotesFile, (UIntPtr)
                apiNotesFile.Length);

        [DllImport("LLVM-C")]
        public static extern ulong LLVMDITypeGetSizeInBits(LLVMMetadataRef dType);

        [DllImport("LLVM-C")]
        public static extern ulong LLVMDITypeGetOffsetInBits(LLVMMetadataRef dType);

        [DllImport("LLVM-C")]
        public static extern uint LLVMDITypeGetAlignInBits(LLVMMetadataRef dType);

        public enum LLVMDIFlags
        {
            Zero = 0,
            Private = 1,
            Protected = 2,
            Public = 3,
            FwdDecl = 1 << 2,
            AppleBlock = 1 << 3,
            ReservedBit4 = 1 << 4,
            Virtual = 1 << 5,
            Artificial = 1 << 6,
            Explicit = 1 << 7,
            Prototyped = 1 << 8,
            ObjcClassComplete = 1 << 9,
            ObjectPointer = 1 << 10,
            Vector = 1 << 11,
            StaticMember = 1 << 12,
            LValueReference = 1 << 13,
            RValueReference = 1 << 14,
            Reserved = 1 << 15,
            SingleInheritance = 1 << 16,
            MultipleInheritance = 2 << 16,
            VirtualInheritance = 3 << 16,
            IntroducedVirtual = 1 << 18,
            BitField = 1 << 19,
            NoReturn = 1 << 20,
            TypePassByValue = 1 << 22,
            TypePassByReference = 1 << 23,
            EnumClass = 1 << 24,
            FixedEnum = EnumClass, // Deprecated.
            Thunk = 1 << 25,
            NonTrivial = 1 << 26,
            BigEndian = 1 << 27,
            LittleEndian = 1 << 28,
            IndirectVirtualBase = (1 << 2) | (1 << 5),

            Accessibility = Private | Protected |
                            Public,

            PtrToMemberRep = SingleInheritance |
                             MultipleInheritance |
                             VirtualInheritance
        }

        public enum LLVMDWARFTag
        {
            subroutine_type = 0x0015
        }

        public enum LLVMDWARFTypeEncoding
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

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateBasicType(LLVMDIBuilderRef builder, string name,
            UIntPtr nameLen, ulong sizeInBits, LLVMDWARFTypeEncoding encoding, LLVMDIFlags flags);

        public static LLVMMetadataRef LLVMDIBuilderCreateBasicType(LLVMDIBuilderRef builder, string name,
            ulong sizeInBits, LLVMDWARFTypeEncoding encoding, LLVMDIFlags flags) =>
            LLVMDIBuilderCreateBasicType(builder, name, (UIntPtr) name.Length, sizeInBits, encoding, flags);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateStructType(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, UIntPtr nameLen, LLVMMetadataRef file, uint lineNumber,
            ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef derivedFrom,
            LLVMMetadataRef[] elements, uint numElements, uint runTimeLang, LLVMMetadataRef vTableHolder,
            string uniqueId, UIntPtr uniqueIdLen);

        public static LLVMMetadataRef LLVMDIBuilderCreateStructType(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint lineNumber, ulong sizeInBits,
            uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef derivedFrom, LLVMMetadataRef[] elements,
            uint runTimeLang, LLVMMetadataRef vTableHolder, string uniqueId) =>
            LLVMDIBuilderCreateStructType(builder, scope, name, (UIntPtr) name.Length, file, lineNumber,
                sizeInBits, alignInBits, flags, derivedFrom, elements, (uint) elements.Length, runTimeLang,
                vTableHolder, uniqueId, (UIntPtr) uniqueId.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateSubroutineType(LLVMDIBuilderRef builder,
            LLVMMetadataRef file, LLVMMetadataRef[] parameterTypes, uint numParameterTypes, LLVMDIFlags flags);

        public static LLVMMetadataRef LLVMDIBuilderCreateSubroutineType(LLVMDIBuilderRef builder,
            LLVMMetadataRef file, LLVMMetadataRef[] parameterTypes, LLVMDIFlags flags) =>
            LLVMDIBuilderCreateSubroutineType(builder, file, parameterTypes, (uint) parameterTypes.Length,
                flags);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateReplaceableCompositeType(LLVMDIBuilderRef builder,
            LLVMDWARFTag tag, string name, UIntPtr nameLen, LLVMMetadataRef scope, LLVMMetadataRef file, uint line,
            uint runtimeLang, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, string uniqueIdentifier,
            UIntPtr uniqueIdentifierLen);

        public static LLVMMetadataRef LLVMDIBuilderCreateReplaceableCompositeType(LLVMDIBuilderRef builder,
            LLVMDWARFTag tag, string name, LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint runtimeLang,
            ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, string uniqueIdentifier) =>
            LLVMDIBuilderCreateReplaceableCompositeType(builder, tag, name, (UIntPtr) name.Length, scope,
                file, line, runtimeLang, sizeInBits, alignInBits, flags, uniqueIdentifier,
                (UIntPtr) uniqueIdentifier.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreatePointerType(LLVMDIBuilderRef builder,
            LLVMMetadataRef pointeeTy, ulong sizeInBits, uint alignInBits, uint addressSpace, string name,
            UIntPtr nameLen);

        public static LLVMMetadataRef LLVMDIBuilderCreatePointerType(LLVMDIBuilderRef builder,
            LLVMMetadataRef pointeeTy, ulong sizeInBits, uint alignInBits, uint addressSpace, string name) =>
            LLVMDIBuilderCreatePointerType(builder, pointeeTy, sizeInBits, alignInBits, addressSpace, name,
                (UIntPtr) name.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateFile(LLVMDIBuilderRef builder, string filename,
            UIntPtr filenameLen, string directory, UIntPtr directoryLen);

        public static LLVMMetadataRef LLVMDIBuilderCreateFile(LLVMDIBuilderRef builder, string filename,
            string directory) =>
            LLVMDIBuilderCreateFile(builder, filename, (UIntPtr) filename.Length, directory,
                (UIntPtr) directory.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateFunction(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, UIntPtr nameLen, string linkageName, UIntPtr linkageNameLen,
            LLVMMetadataRef file, uint lineNo, LLVMMetadataRef ty, bool isLocalToUnit, bool isDefinition,
            uint scopeLine, LLVMDIFlags flags, bool isOptimized);

        public static LLVMMetadataRef LLVMDIBuilderCreateFunction(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, string linkageName, LLVMMetadataRef file, uint lineNo,
            LLVMMetadataRef ty, bool isLocalToUnit, bool isDefinition, uint scopeLine, LLVMDIFlags flags,
            bool isOptimized) =>
            LLVMDIBuilderCreateFunction(builder, scope, name, (UIntPtr) name.Length, linkageName,
                (UIntPtr) linkageName.Length, file, lineNo, ty, isLocalToUnit, isDefinition, scopeLine, flags,
                isOptimized);

        [DllImport("LLVM-C")]
        public static extern LLVMMetadataRef LLVMDIBuilderCreateLexicalBlock(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint column);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateEnumerator(LLVMDIBuilderRef builder, string name,
            UIntPtr nameLen, long value, bool isUnsigned);

        public static LLVMMetadataRef LLVMDIBuilderCreateEnumerator(LLVMDIBuilderRef builder, string name,
            long value, bool isUnsigned) =>
            LLVMDIBuilderCreateEnumerator(builder, name, (UIntPtr) name.Length, value, isUnsigned);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateEnumerationType(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, UIntPtr nameLen, LLVMMetadataRef file, uint lineNumber,
            ulong sizeInBits, uint alignInBits, LLVMMetadataRef[] elements, uint numElements,
            LLVMMetadataRef classTy);

        public static LLVMMetadataRef LLVMDIBuilderCreateEnumerationType(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint lineNumber, ulong sizeInBits,
            uint alignInBits, LLVMMetadataRef[] elements, LLVMMetadataRef classTy) =>
            LLVMDIBuilderCreateEnumerationType(builder, scope, name, (UIntPtr) name.Length, file, lineNumber,
                sizeInBits, alignInBits, elements, (uint) elements.Length, classTy);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateExpression(LLVMDIBuilderRef builder, long[] addr,
            UIntPtr length);

        public static LLVMMetadataRef LLVMDIBuilderCreateExpression(LLVMDIBuilderRef builder, long[] addr) =>
            LLVMDIBuilderCreateExpression(builder, addr, (UIntPtr) addr.Length);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateAutoVariable(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, UIntPtr nameLen, LLVMMetadataRef file, uint lineNo,
            LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags flags, uint alignInBits);

        public static LLVMMetadataRef LLVMDIBuilderCreateAutoVariable(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint lineNo, LLVMMetadataRef ty,
            bool alwaysPreserve, LLVMDIFlags flags, uint alignInBits) =>
            LLVMDIBuilderCreateAutoVariable(builder, scope, name, (UIntPtr) name.Length, file, lineNo, ty,
                alwaysPreserve, flags, alignInBits);

        [DllImport("LLVM-C")]
        private static extern LLVMMetadataRef LLVMDIBuilderCreateParameterVariable(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, UIntPtr nameLen, uint argNo, LLVMMetadataRef file, uint lineNo,
            LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags flags);

        public static LLVMMetadataRef LLVMDIBuilderCreateParameterVariable(LLVMDIBuilderRef builder,
            LLVMMetadataRef scope, string name, uint argNo, LLVMMetadataRef file, uint lineNo, LLVMMetadataRef ty,
            bool alwaysPreserve, LLVMDIFlags flags) =>
            LLVMDIBuilderCreateParameterVariable(builder, scope, name, (UIntPtr) name.Length, argNo, file,
                lineNo, ty, alwaysPreserve, flags);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMDIBuilderInsertDeclareAtEnd(LLVMDIBuilderRef builder,
            LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc,
            LLVMBasicBlockRef block);

        [DllImport("LLVM-C")]
        public static extern LLVMValueRef LLVMDIBuilderInsertDbgValueAtEnd(LLVMDIBuilderRef builder,
            LLVMValueRef val, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc,
            LLVMBasicBlockRef block);

        [DllImport("LLVM-C")]
        public static extern LLVMMetadataRef LLVMDIBuilderCreateDebugLocation(LLVMContextRef ctx, uint line,
            uint column, LLVMMetadataRef scope, LLVMMetadataRef inlinedAt);


        public struct LLVMPassManagerRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMPassManagerRef LLVMCreatePassManager();

        [DllImport("LLVM-C")]
        public static extern LLVMPassManagerRef LLVMCreateFunctionPassManagerForModule(LLVMModuleRef m);

        [DllImport("LLVM-C")]
        public static extern bool LLVMRunPassManager(LLVMPassManagerRef pm, LLVMModuleRef m);

        [DllImport("LLVM-C")]
        public static extern bool LLVMInitializeFunctionPassManager(LLVMPassManagerRef fpm);

        [DllImport("LLVM-C")]
        public static extern bool LLVMRunFunctionPassManager(LLVMPassManagerRef fpm, LLVMValueRef f);

        [DllImport("LLVM-C")]
        public static extern bool LLVMFinalizeFunctionPassManager(LLVMPassManagerRef fpm);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposePassManager(LLVMPassManagerRef pm);

        public struct LLVMPassManagerBuilderRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMPassManagerBuilderRef LLVMPassManagerBuilderCreate();

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderDispose(LLVMPassManagerBuilderRef pmb);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderSetOptLevel(LLVMPassManagerBuilderRef pmb, uint optLevel);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderSetSizeLevel(LLVMPassManagerBuilderRef pmb, uint sizeLevel);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderSetDisableUnrollLoops(LLVMPassManagerBuilderRef pmb,
            bool value);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderUseInlinerWithThreshold(LLVMPassManagerBuilderRef pmb,
            uint threshold);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderPopulateFunctionPassManager(LLVMPassManagerBuilderRef pmb,
            LLVMPassManagerRef pm);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderPopulateModulePassManager(LLVMPassManagerBuilderRef pmb,
            LLVMPassManagerRef pm);

        [DllImport("LLVM-C")]
        public static extern void LLVMPassManagerBuilderPopulateLTOPassManager(LLVMPassManagerBuilderRef pmb,
            LLVMPassManagerRef pm, bool internalize, bool runInliner);


        public enum LLVMCodeGenOptLevel
        {
            None,
            Less,
            Default,
            Aggressive
        }

        public enum LLVMRelocMode
        {
            Default,
            Static,
            PIC,
            DynamicNoPic,
            ROPI,
            RWPI,
            ROPI_RWPI
        }

        public enum LLVMCodeModel
        {
            Default,
            JITDefault,
            Tiny,
            Small,
            Kernel,
            Medium,
            Large
        }

        public enum LLVMCodeGenFileType
        {
            AssemblyFile,
            ObjectFile
        }

        public struct LLVMTargetRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTargetRef LLVMGetTargetFromName(string name);

        [DllImport("LLVM-C")]
        private static extern bool LLVMGetTargetFromTriple(string triple, out LLVMTargetRef t, out IntPtr errorMessage);

        public static bool LLVMGetTargetFromTriple(string triple, out LLVMTargetRef t, out string? errorMessage)
        {
            if (LLVMGetTargetFromTriple(triple, out t, out IntPtr errorMessagePtr)) {
                errorMessage = MarshallMessage(errorMessagePtr);
                return true;
            }

            errorMessage = null;
            return false;
        }

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTargetName(LLVMTargetRef t);

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTargetDescription(LLVMTargetRef t);

        [DllImport("LLVM-C")]
        public static extern bool LLVMTargetHasJIT(LLVMTargetRef t);

        [DllImport("LLVM-C")]
        public static extern bool LLVMTargetHasTargetMachine(LLVMTargetRef t);

        [DllImport("LLVM-C")]
        public static extern bool LLVMTargetHasAsmBackend(LLVMTargetRef t);

        public struct LLVMTargetMachineRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTargetMachineRef LLVMCreateTargetMachine(LLVMTargetRef t, string triple, string cpu,
            string features, LLVMCodeGenOptLevel level, LLVMRelocMode reloc, LLVMCodeModel codeModel);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeTargetMachine(LLVMTargetMachineRef t);

        [DllImport("LLVM-C")]
        public static extern LLVMTargetRef LLVMGetTargetMachineTarget(LLVMTargetMachineRef t);

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTargetMachineTriple(LLVMTargetMachineRef t);

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTargetMachineCPU(LLVMTargetMachineRef t);

        [DllImport("LLVM-C")]
        public static extern string LLVMGetTargetMachineFeatureString(LLVMTargetMachineRef t);

        public struct LLVMTargetDataRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMTargetDataRef LLVMCreateTargetDataLayout(LLVMTargetMachineRef t);

        [DllImport("LLVM-C")]
        public static extern void LLVMSetTargetMachineAsmVerbosity(LLVMTargetMachineRef t, bool verboseAsm);

        [DllImport("LLVM-C")]
        private static extern bool LLVMTargetMachineEmitToFile(LLVMTargetMachineRef t, LLVMModuleRef m, string filename,
            LLVMCodeGenFileType codegen, out IntPtr errorMessage);

        public static bool LLVMTargetMachineEmitToFile(LLVMTargetMachineRef t, LLVMModuleRef m, string filename,
            LLVMCodeGenFileType codegen, out string? errorMessage)
        {
            if (LLVMTargetMachineEmitToFile(t, m, filename, codegen, out IntPtr errorMessagePtr)) {
                errorMessage = MarshallMessage(errorMessagePtr);
                return true;
            }

            errorMessage = null;
            return false;
        }

        [DllImport("LLVM-C")]
        private static extern bool LLVMTargetMachineEmitToMemoryBuffer(LLVMTargetMachineRef t, LLVMModuleRef m,
            LLVMCodeGenFileType codegen, out IntPtr errorMessage, out LLVMMemoryBufferRef outMemBuf);

        public static bool LLVMTargetMachineEmitToMemoryBuffer(LLVMTargetMachineRef t, LLVMModuleRef m,
            LLVMCodeGenFileType codegen, out string? errorMessage, out LLVMMemoryBufferRef outMemBuf)
        {
            if (LLVMTargetMachineEmitToMemoryBuffer(t, m, codegen, out IntPtr errorMessagePtr, out outMemBuf)) {
                errorMessage = MarshallMessage(errorMessagePtr);
                return true;
            }

            errorMessage = null;
            return false;
        }


        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetDefaultTargetTriple();

        public static string LLVMGetDefaultTargetTripleString() =>
            MarshallMessage(LLVMGetDefaultTargetTriple());

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMNormalizeTargetTriple(string triple);

        public static string LLVMNormalizeTargetTripleString(string triple) =>
            MarshallMessage(LLVMNormalizeTargetTriple(triple));

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetHostCPUName();

        public static string LLVMGetHostCPUNameString() => MarshallMessage(LLVMGetHostCPUName());

        [DllImport("LLVM-C")]
        private static extern IntPtr LLVMGetHostCPUFeatures();

        public static string LLVMGetHostCPUFeaturesString() => MarshallMessage(LLVMGetHostCPUFeatures());


        [DllImport("LLVM-C")]
        public static extern bool LLVMLinkModules2(LLVMModuleRef dest, LLVMModuleRef src);


        public struct LLVMGenericValueRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        public static extern LLVMGenericValueRef LLVMCreateGenericValueOfInt(LLVMTypeRef ty, ulong n, bool isSigned);

        [DllImport("LLVM-C")]
        public static extern LLVMGenericValueRef LLVMCreateGenericValueOfPointer(IntPtr p);

        [DllImport("LLVM-C")]
        public static extern LLVMGenericValueRef LLVMCreateGenericValueOfFloat(LLVMTypeRef ty, double n);

        [DllImport("LLVM-C")]
        public static extern uint LLVMGenericValueIntWidth(LLVMGenericValueRef genValRef);

        [DllImport("LLVM-C")]
        public static extern ulong LLVMGenericValueToInt(LLVMGenericValueRef genVal, bool isSigned);

        [DllImport("LLVM-C")]
        public static extern IntPtr LLVMGenericValueToPointer(LLVMGenericValueRef genVal);

        [DllImport("LLVM-C")]
        public static extern double LLVMGenericValueToFloat(LLVMTypeRef tyRef, LLVMGenericValueRef genVal);

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeGenericValue(LLVMGenericValueRef genVal);


        public struct LLVMExecutionEngineRef
        {
            private IntPtr InternalPtr;
            public bool IsValid => InternalPtr != IntPtr.Zero;
        }

        [DllImport("LLVM-C")]
        private static extern bool LLVMCreateExecutionEngineForModule(out LLVMExecutionEngineRef outEE, LLVMModuleRef m,
            out IntPtr errorOutBuf);

        public static bool LLVMCreateExecutionEngineForModule(out LLVMExecutionEngineRef outEE, LLVMModuleRef m,
            out string? errorMessage)
        {
            bool ret = LLVMCreateExecutionEngineForModule(out outEE, m, out IntPtr errPtr);
            errorMessage = MarshallMessage(errPtr);
            return ret;
        }

        [DllImport("LLVM-C")]
        public static extern void LLVMDisposeExecutionEngine(LLVMExecutionEngineRef ee);

        [DllImport("LLVM-C")]
        public static extern bool LLVMFindFunction(LLVMExecutionEngineRef ee, string name, out LLVMValueRef outFn);

        [DllImport("LLVM-C")]
        private static extern LLVMGenericValueRef LLVMRunFunction(LLVMExecutionEngineRef ee, LLVMValueRef f,
            uint numArgs, LLVMGenericValueRef[] args);

        public static LLVMGenericValueRef LLVMRunFunction(LLVMExecutionEngineRef ee, LLVMValueRef f,
            LLVMGenericValueRef[] args) =>
            LLVMRunFunction(ee, f, (uint) args.Length, args);



        public static void Main(string[] args)
        {
            LLVMContextRef context = LLVMContextCreate();

            LLVMContextSetDiagnosticHandler(context, (diagnosticInfo, diagnosticContext) => {
                var severity = LLVMGetDiagInfoSeverity(diagnosticInfo);
                var description = LLVMGetDiagInfoDescriptionString(diagnosticInfo);
                Console.Write(severity + " " + description);
            }, IntPtr.Zero);

            LLVMModuleRef module = LLVMModuleCreateWithNameInContext("MyModule", context);

            LLVMTypeRef int32Type = LLVMInt32TypeInContext(context);
            LLVMTypeRef funcType = LLVMFunctionType(int32Type, new LLVMTypeRef[] { }, 0, false);
            LLVMValueRef function = LLVMAddFunction(module, "GetDragonsBankBalance", funcType);

            LLVMBasicBlockRef basicBlock = LLVMAppendBasicBlockInContext(context, function);
            LLVMBasicBlockRef basicBlock2 = LLVMAppendBasicBlockInContext(context, function);

            LLVMBuilderRef builder = LLVMCreateBuilderInContext(context);
            LLVMPositionBuilderAtEnd(builder, basicBlock);
            LLVMValueRef theAnswer = LLVMConstInt(int32Type, int.MaxValue, true);
            LLVMBuildRet(builder, theAnswer);

            LLVMBasicBlockRef[] basicBlockRefs = LLVMGetBasicBlocks(function);

            LLVMDumpModule(module);

            LLVMDisposeBuilder(builder);
            LLVMDisposeModule(module);
            LLVMContextDispose(context);
        }
    }
}
