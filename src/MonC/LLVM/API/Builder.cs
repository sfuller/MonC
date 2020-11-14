using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public sealed class Builder : IDisposable
    {
        private LLVMBuilderRef _builder;

        internal Builder(LLVMContextRef context) => _builder = LLVMBuilderRef.Create(context);

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            _builder.Dispose();
        }

        ~Builder() => DoDispose();

        public void Position(BasicBlock block, Value instr) => _builder.Position(block, instr);

        public void PositionBefore(Value instr) => _builder.PositionBefore(instr);

        public void PositionAtEnd(BasicBlock block) => _builder.PositionAtEnd(block);

        public BasicBlock InsertBlock => _builder.InsertBlock;

        public unsafe void InsertExistingBasicBlockAfterInsertBlock(BasicBlock bb) =>
            LLVMSharp.Interop.LLVM.InsertExistingBasicBlockAfterInsertBlock(_builder, (LLVMBasicBlockRef) bb);

        public void ClearInsertionPosition() => _builder.ClearInsertionPosition();

        public void Insert(Value instr) => _builder.Insert(instr);

        public void InsertWithName(Value instr, string name) => _builder.InsertWithName(instr, name);

        public Value BuildRetVoid() => _builder.BuildRetVoid();

        public Value BuildRet(Value value) => _builder.BuildRet(value);

        public Value BuildAggregateRet(Value[] retVals) => _builder.BuildAggregateRet(
            Array.ConvertAll(retVals, val => (LLVMValueRef) val));

        public Value BuildBr(BasicBlock dest) => _builder.BuildBr(dest);

        public Value BuildCondBr(Value ifVal, BasicBlock thenBlock, BasicBlock elseBlock) =>
            _builder.BuildCondBr(ifVal, thenBlock, elseBlock);

        public Value BuildSwitch(Value val, BasicBlock elseBlock, uint numCases) =>
            _builder.BuildSwitch(val, elseBlock, numCases);

        public Value BuildIndirectBr(Value addr, uint numDests) => _builder.BuildIndirectBr(addr, numDests);

        public Value BuildAdd(Value lhs, Value rhs, string name = "") => _builder.BuildAdd(lhs, rhs, name);

        public Value BuildNSWAdd(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNSWAdd(lhs, rhs, name);

        public Value BuildNUWAdd(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNUWAdd(lhs, rhs, name);

        public Value BuildFAdd(Value lhs, Value rhs, string name = "") => _builder.BuildFAdd(lhs, rhs, name);

        public Value BuildSub(Value lhs, Value rhs, string name = "") => _builder.BuildSub(lhs, rhs, name);

        public Value BuildNSWSub(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNSWSub(lhs, rhs, name);

        public Value BuildNUWSub(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNUWSub(lhs, rhs, name);

        public Value BuildFSub(Value lhs, Value rhs, string name = "") => _builder.BuildFSub(lhs, rhs, name);

        public Value BuildMul(Value lhs, Value rhs, string name = "") => _builder.BuildMul(lhs, rhs, name);

        public Value BuildNSWMul(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNSWMul(lhs, rhs, name);

        public Value BuildNUWMul(Value lhs, Value rhs, string name = "") =>
            _builder.BuildNUWMul(lhs, rhs, name);

        public Value BuildFMul(Value lhs, Value rhs, string name = "") => _builder.BuildFMul(lhs, rhs, name);

        public Value BuildUDiv(Value lhs, Value rhs, string name = "") => _builder.BuildUDiv(lhs, rhs, name);

        public Value BuildSDiv(Value lhs, Value rhs, string name = "") => _builder.BuildSDiv(lhs, rhs, name);

        public Value BuildExactSDiv(Value lhs, Value rhs, string name = "") =>
            _builder.BuildExactSDiv(lhs, rhs, name);

        public Value BuildFDiv(Value lhs, Value rhs, string name = "") => _builder.BuildFDiv(lhs, rhs, name);

        public Value BuildURem(Value lhs, Value rhs, string name = "") => _builder.BuildURem(lhs, rhs, name);

        public Value BuildSRem(Value lhs, Value rhs, string name = "") => _builder.BuildSRem(lhs, rhs, name);

        public Value BuildFRem(Value lhs, Value rhs, string name = "") => _builder.BuildFRem(lhs, rhs, name);

        public Value BuildShl(Value lhs, Value rhs, string name = "") => _builder.BuildShl(lhs, rhs, name);

        public Value BuildLShr(Value lhs, Value rhs, string name = "") => _builder.BuildLShr(lhs, rhs, name);

        public Value BuildAShr(Value lhs, Value rhs, string name = "") => _builder.BuildAShr(lhs, rhs, name);

        public Value BuildAnd(Value lhs, Value rhs, string name = "") => _builder.BuildAnd(lhs, rhs, name);

        public Value BuildOr(Value lhs, Value rhs, string name = "") => _builder.BuildOr(lhs, rhs, name);

        public Value BuildXor(Value lhs, Value rhs, string name = "") => _builder.BuildXor(lhs, rhs, name);

        public Value BuildBinOp(LLVMOpcode op, Value lhs, Value rhs, string name = "") =>
            _builder.BuildBinOp(op, lhs, rhs, name);

        public Value BuildNeg(Value v, string name = "") => _builder.BuildNeg(v, name);

        public Value BuildNSWNeg(Value v, string name = "") => _builder.BuildNSWNeg(v, name);

        public Value BuildNUWNeg(Value v, string name = "") => _builder.BuildNUWNeg(v, name);

        public Value BuildFNeg(Value v, string name = "") => _builder.BuildFNeg(v, name);

        public Value BuildNot(Value v, string name = "") => _builder.BuildNot(v, name);

        public Value BuildCast(LLVMOpcode op, Value val, Type destTy, string name = "") =>
            _builder.BuildCast(op, val, destTy, name);

        public Value BuildPointerCast(Value val, Type destTy, string name = "") =>
            _builder.BuildPointerCast(val, destTy, name);

        public Value BuildICmp(LLVMIntPredicate op, Value lhs, Value rhs, string name = "") =>
            _builder.BuildICmp(op, lhs, rhs, name);

        public Value BuildFCmp(LLVMRealPredicate op, Value lhs, Value rhs, string name = "") =>
            _builder.BuildFCmp(op, lhs, rhs, name);

        public Value BuildPhi(Type ty, string name = "") => _builder.BuildPhi(ty, name);

        public Value BuildCall(Value fn, Value[] args, string name = "") => _builder.BuildCall(
            fn, Array.ConvertAll(args, a => (LLVMValueRef) a), name);

        public Value BuildSelect(Value _if, Value then, Value _else, string name = "") =>
            _builder.BuildSelect(_if, then, _else, name);

        public Value BuildAlloca(Type ty, string name = "") => _builder.BuildAlloca(ty, name);

        public Value BuildArrayAlloca(Type ty, Value val, string name = "") =>
            _builder.BuildArrayAlloca(ty, val, name);

        public Value BuildLoad(Value ptr, string name = "") => _builder.BuildLoad(ptr, name);

        public Value BuildStore(Value val, Value ptr) => _builder.BuildStore(val, ptr);

        public Value BuildStructGEP(Value pointer, uint idx, string name = "") =>
            _builder.BuildStructGEP(pointer, idx, name);

        public Value BuildExtractValue(Value aggVal, uint index, string name = "") =>
            _builder.BuildExtractValue(aggVal, index, name);

        public Value BuildInsertValue(Value aggVal, Value eltVal, uint index, string name = "") =>
            _builder.BuildInsertValue(aggVal, eltVal, index, name);

        public Value BuildGlobalString(string str, string name = "") => _builder.BuildGlobalString(str, name);

        public Value BuildGlobalStringPtr(string str, string name = "") =>
            _builder.BuildGlobalStringPtr(str, name);

        public unsafe void SetCurrentDebugLocation(Metadata loc) =>
            LLVMSharp.Interop.LLVM.SetCurrentDebugLocation2(_builder, (LLVMMetadataRef) loc);

        /// <summary>
        /// Port of CastInst::getCastOpcode
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public static LLVMOpcode GetCastOpcode(Value src, bool srcIsSigned, Type destTy, bool destIsSigned)
        {
            Type srcTy = src.TypeOf;

            if (!srcTy.IsFirstClassType() || !destTy.IsFirstClassType())
                throw new InvalidCastException("Only first class types are castable!");

            if (srcTy == destTy)
                return LLVMOpcode.LLVMBitCast;

            // FIXME: Check address space sizes here
            if (srcTy.Kind == LLVMTypeKind.LLVMVectorTypeKind)
                if (destTy.Kind == LLVMTypeKind.LLVMVectorTypeKind)
                    if (srcTy.VectorSize == destTy.VectorSize) {
                        // An element by element cast.  Find the appropriate opcode based on the
                        // element types.
                        srcTy = srcTy.ElementType;
                        destTy = destTy.ElementType;
                    }

            // Get the bit sizes, we'll need these
            uint srcBits = srcTy.GetPrimitiveSizeInBits(); // 0 for ptr
            uint destBits = destTy.GetPrimitiveSizeInBits(); // 0 for ptr

            // Run through the possibilities ...
            if (destTy.Kind == LLVMTypeKind.LLVMIntegerTypeKind) { // Casting to integral
                if (srcTy.Kind == LLVMTypeKind.LLVMIntegerTypeKind) { // Casting from integral
                    if (destBits < srcBits)
                        return LLVMOpcode.LLVMTrunc; // int -> smaller int
                    else if (destBits > srcBits) { // its an extension
                        if (srcIsSigned)
                            return LLVMOpcode.LLVMSExt; // signed -> SEXT
                        else
                            return LLVMOpcode.LLVMZExt; // unsigned -> ZEXT
                    } else {
                        return LLVMOpcode.LLVMBitCast; // Same size, No-op cast
                    }
                } else if (srcTy.IsFloatingPointType()) { // Casting from floating pt
                    if (destIsSigned)
                        return LLVMOpcode.LLVMFPToSI; // FP -> sint
                    else
                        return LLVMOpcode.LLVMFPToUI; // FP -> uint
                } else if (srcTy.Kind == LLVMTypeKind.LLVMVectorTypeKind) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector to integer of different width");
                    return LLVMOpcode.LLVMBitCast; // Same size, no-op cast
                } else {
                    if (srcTy.Kind != LLVMTypeKind.LLVMPointerTypeKind)
                        throw new InvalidCastException("Casting from a value that is not first-class type");
                    return LLVMOpcode.LLVMPtrToInt; // ptr -> int
                }
            } else if (srcTy.IsFloatingPointType()) { // Casting to floating pt
                if (srcTy.Kind == LLVMTypeKind.LLVMIntegerTypeKind) { // Casting from integral
                    if (srcIsSigned)
                        return LLVMOpcode.LLVMSIToFP; // sint -> FP
                    else
                        return LLVMOpcode.LLVMUIToFP; // uint -> FP
                } else if (srcTy.IsFloatingPointType()) { // Casting from floating pt
                    if (destBits < srcBits) {
                        return LLVMOpcode.LLVMFPTrunc; // FP -> smaller FP
                    } else if (destBits > srcBits) {
                        return LLVMOpcode.LLVMFPExt; // FP -> larger FP
                    } else {
                        return LLVMOpcode.LLVMBitCast; // same size, no-op cast
                    }
                } else if (srcTy.Kind == LLVMTypeKind.LLVMVectorTypeKind) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector to floating point of different width");
                    return LLVMOpcode.LLVMBitCast; // same size, no-op cast
                }

                throw new InvalidCastException("Casting pointer or non-first class to float");
            } else if (destTy.Kind == LLVMTypeKind.LLVMVectorTypeKind) {
                if (destBits != srcBits)
                    throw new InvalidCastException("Illegal cast to vector (wrong type or size)");
                return LLVMOpcode.LLVMBitCast;
            } else if (destTy.Kind == LLVMTypeKind.LLVMPointerTypeKind) {
                if (srcTy.Kind == LLVMTypeKind.LLVMPointerTypeKind) {
                    if (destTy.PointerAddressSpace != srcTy.PointerAddressSpace)
                        return LLVMOpcode.LLVMAddrSpaceCast;
                    return LLVMOpcode.LLVMBitCast; // ptr -> ptr
                } else if (srcTy.Kind == LLVMTypeKind.LLVMIntegerTypeKind) {
                    return LLVMOpcode.LLVMIntToPtr; // int -> ptr
                }

                throw new InvalidCastException("Casting pointer to other than pointer or int");
            } else if (destTy.Kind == LLVMTypeKind.LLVMX86_MMXTypeKind) {
                if (srcTy.Kind == LLVMTypeKind.LLVMVectorTypeKind) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector of wrong width to X86_MMX");
                    return LLVMOpcode.LLVMBitCast; // 64-bit vector to MMX
                }

                throw new InvalidCastException("Illegal cast to X86_MMX");
            }

            throw new InvalidCastException("Casting to type that is not first-class");
        }
    }
}
