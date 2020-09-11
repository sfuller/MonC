using System;
using System.Text.RegularExpressions;

namespace MonC.LLVM
{
    public sealed class Builder : IDisposable
    {
        private CAPI.LLVMBuilderRef _builder;

        internal Builder(CAPI.LLVMContextRef context) => _builder = CAPI.LLVMCreateBuilderInContext(context);

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_builder.IsValid) {
                CAPI.LLVMDisposeBuilder(_builder);
                _builder = new CAPI.LLVMBuilderRef();
            }
        }

        ~Builder() => DoDispose();

        public void Position(BasicBlock block, Value instr) => CAPI.LLVMPositionBuilder(_builder, block, instr);

        public void PositionBefore(Value instr) => CAPI.LLVMPositionBuilderBefore(_builder, instr);

        public void PositionAtEnd(BasicBlock block) => CAPI.LLVMPositionBuilderAtEnd(_builder, block);

        public BasicBlock InsertBlock => CAPI.LLVMGetInsertBlock(_builder);

        public void InsertExistingBasicBlockAfterInsertBlock(BasicBlock bb) =>
            CAPI.LLVMInsertExistingBasicBlockAfterInsertBlock(_builder, bb);

        public void ClearInsertionPosition() => CAPI.LLVMClearInsertionPosition(_builder);

        public void Insert(Value instr) => CAPI.LLVMInsertIntoBuilder(_builder, instr);

        public void InsertWithName(Value instr, string name) =>
            CAPI.LLVMInsertIntoBuilderWithName(_builder, instr, name);

        public Value BuildRetVoid() => CAPI.LLVMBuildRetVoid(_builder);

        public Value BuildRet(Value value) => CAPI.LLVMBuildRet(_builder, value);

        public Value BuildAggregateRet(Value[] retVals) =>
            CAPI.LLVMBuildAggregateRet(_builder, Array.ConvertAll(retVals, val => (CAPI.LLVMValueRef) val));

        public Value BuildBr(BasicBlock dest) => CAPI.LLVMBuildBr(_builder, dest);

        public Value BuildCondBr(Value ifVal, BasicBlock thenBlock, BasicBlock elseBlock) =>
            CAPI.LLVMBuildCondBr(_builder, ifVal, thenBlock, elseBlock);

        public Value BuildSwitch(Value val, BasicBlock elseBlock, uint numCases) =>
            CAPI.LLVMBuildSwitch(_builder, val, elseBlock, numCases);

        public Value BuildIndirectBr(Value addr, uint numDests) => CAPI.LLVMBuildIndirectBr(_builder, addr, numDests);

        public Value BuildAdd(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildAdd(_builder, lhs, rhs, name);

        public Value BuildNSWAdd(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNSWAdd(_builder, lhs, rhs, name);

        public Value BuildNUWAdd(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNUWAdd(_builder, lhs, rhs, name);

        public Value BuildFAdd(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildFAdd(_builder, lhs, rhs, name);

        public Value BuildSub(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildSub(_builder, lhs, rhs, name);

        public Value BuildNSWSub(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNSWSub(_builder, lhs, rhs, name);

        public Value BuildNUWSub(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNUWSub(_builder, lhs, rhs, name);

        public Value BuildFSub(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildFSub(_builder, lhs, rhs, name);

        public Value BuildMul(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildMul(_builder, lhs, rhs, name);

        public Value BuildNSWMul(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNSWMul(_builder, lhs, rhs, name);

        public Value BuildNUWMul(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildNUWMul(_builder, lhs, rhs, name);

        public Value BuildFMul(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildFMul(_builder, lhs, rhs, name);

        public Value BuildUDiv(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildUDiv(_builder, lhs, rhs, name);

        public Value BuildExactUDiv(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildExactUDiv(_builder, lhs, rhs, name);

        public Value BuildSDiv(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildSDiv(_builder, lhs, rhs, name);

        public Value BuildExactSDiv(Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildExactSDiv(_builder, lhs, rhs, name);

        public Value BuildFDiv(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildFDiv(_builder, lhs, rhs, name);

        public Value BuildURem(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildURem(_builder, lhs, rhs, name);

        public Value BuildSRem(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildSRem(_builder, lhs, rhs, name);

        public Value BuildFRem(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildFRem(_builder, lhs, rhs, name);

        public Value BuildShl(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildShl(_builder, lhs, rhs, name);

        public Value BuildLShr(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildLShr(_builder, lhs, rhs, name);

        public Value BuildAShr(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildAShr(_builder, lhs, rhs, name);

        public Value BuildAnd(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildAnd(_builder, lhs, rhs, name);

        public Value BuildOr(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildOr(_builder, lhs, rhs, name);

        public Value BuildXor(Value lhs, Value rhs, string name = "") => CAPI.LLVMBuildXor(_builder, lhs, rhs, name);

        public Value BuildBinOp(CAPI.LLVMOpcode op, Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildBinOp(_builder, op, lhs, rhs, name);

        public Value BuildNeg(Value v, string name = "") => CAPI.LLVMBuildNeg(_builder, v, name);

        public Value BuildNSWNeg(Value v, string name = "") => CAPI.LLVMBuildNSWNeg(_builder, v, name);

        public Value BuildNUWNeg(Value v, string name = "") => CAPI.LLVMBuildNUWNeg(_builder, v, name);

        public Value BuildFNeg(Value v, string name = "") => CAPI.LLVMBuildFNeg(_builder, v, name);

        public Value BuildNot(Value v, string name = "") => CAPI.LLVMBuildNot(_builder, v, name);

        public Value BuildCast(CAPI.LLVMOpcode op, Value val, Type destTy, string name = "") =>
            CAPI.LLVMBuildCast(_builder, op, val, destTy, name);

        public Value BuildPointerCast(Value val, Type destTy, string name = "") =>
            CAPI.LLVMBuildPointerCast(_builder, val, destTy, name);

        public Value BuildICmp(CAPI.LLVMIntPredicate op, Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildICmp(_builder, op, lhs, rhs, name);

        public Value BuildFCmp(CAPI.LLVMRealPredicate op, Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildFCmp(_builder, op, lhs, rhs, name);

        public Value BuildPhi(Type ty, string name = "") => CAPI.LLVMBuildPhi(_builder, ty, name);

        public Value BuildCall(Type ty, Value fn, Value[] args, string name = "") => CAPI.LLVMBuildCall2(_builder, ty,
            fn, Array.ConvertAll(args, a => (CAPI.LLVMValueRef) a), name);

        public Value BuildSelect(Value _if, Value then, Value _else, string name = "") =>
            CAPI.LLVMBuildSelect(_builder, _if, then, _else, name);

        public Value BuildAlloca(Type ty, string name = "") => CAPI.LLVMBuildAlloca(_builder, ty, name);

        public Value BuildArrayAlloca(Type ty, Value val, string name = "") =>
            CAPI.LLVMBuildArrayAlloca(_builder, ty, val, name);

        public Value BuildLoad(Type ty, Value ptr, string name = "") => CAPI.LLVMBuildLoad2(_builder, ty, ptr, name);

        public Value BuildStore(Value val, Value ptr) => CAPI.LLVMBuildStore(_builder, val, ptr);

        public Value BuildGlobalString(string str, string name = "") => CAPI.LLVMBuildGlobalString(_builder, str, name);

        public Value BuildGlobalStringPtr(string str, string name = "") =>
            CAPI.LLVMBuildGlobalStringPtr(_builder, str, name);

        public void SetCurrentDebugLocation(Metadata loc) => CAPI.LLVMSetCurrentDebugLocation2(_builder, loc);

        /// <summary>
        /// Port of CastInst::getCastOpcode
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public static CAPI.LLVMOpcode GetCastOpcode(Value src, bool srcIsSigned, Type destTy, bool destIsSigned)
        {
            Type srcTy = src.TypeOf;

            if (!srcTy.IsFirstClassType() || !destTy.IsFirstClassType())
                throw new InvalidCastException("Only first class types are castable!");

            if (srcTy == destTy)
                return CAPI.LLVMOpcode.BitCast;

            // FIXME: Check address space sizes here
            if (srcTy.Kind == CAPI.LLVMTypeKind.Vector)
                if (destTy.Kind == CAPI.LLVMTypeKind.Vector)
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
            if (destTy.Kind == CAPI.LLVMTypeKind.Integer) { // Casting to integral
                if (srcTy.Kind == CAPI.LLVMTypeKind.Integer) { // Casting from integral
                    if (destBits < srcBits)
                        return CAPI.LLVMOpcode.Trunc; // int -> smaller int
                    else if (destBits > srcBits) { // its an extension
                        if (srcIsSigned)
                            return CAPI.LLVMOpcode.SExt; // signed -> SEXT
                        else
                            return CAPI.LLVMOpcode.ZExt; // unsigned -> ZEXT
                    } else {
                        return CAPI.LLVMOpcode.BitCast; // Same size, No-op cast
                    }
                } else if (srcTy.IsFloatingPointTy()) { // Casting from floating pt
                    if (destIsSigned)
                        return CAPI.LLVMOpcode.FPToSI; // FP -> sint
                    else
                        return CAPI.LLVMOpcode.FPToUI; // FP -> uint
                } else if (srcTy.Kind == CAPI.LLVMTypeKind.Vector) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector to integer of different width");
                    return CAPI.LLVMOpcode.BitCast; // Same size, no-op cast
                } else {
                    if (srcTy.Kind != CAPI.LLVMTypeKind.Pointer)
                        throw new InvalidCastException("Casting from a value that is not first-class type");
                    return CAPI.LLVMOpcode.PtrToInt; // ptr -> int
                }
            } else if (srcTy.IsFloatingPointTy()) { // Casting to floating pt
                if (srcTy.Kind == CAPI.LLVMTypeKind.Integer) { // Casting from integral
                    if (srcIsSigned)
                        return CAPI.LLVMOpcode.SIToFP; // sint -> FP
                    else
                        return CAPI.LLVMOpcode.UIToFP; // uint -> FP
                } else if (srcTy.IsFloatingPointTy()) { // Casting from floating pt
                    if (destBits < srcBits) {
                        return CAPI.LLVMOpcode.FPTrunc; // FP -> smaller FP
                    } else if (destBits > srcBits) {
                        return CAPI.LLVMOpcode.FPExt; // FP -> larger FP
                    } else {
                        return CAPI.LLVMOpcode.BitCast; // same size, no-op cast
                    }
                } else if (srcTy.Kind == CAPI.LLVMTypeKind.Vector) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector to floating point of different width");
                    return CAPI.LLVMOpcode.BitCast; // same size, no-op cast
                }

                throw new InvalidCastException("Casting pointer or non-first class to float");
            } else if (destTy.Kind == CAPI.LLVMTypeKind.Vector) {
                if (destBits != srcBits)
                    throw new InvalidCastException("Illegal cast to vector (wrong type or size)");
                return CAPI.LLVMOpcode.BitCast;
            } else if (destTy.Kind == CAPI.LLVMTypeKind.Pointer) {
                if (srcTy.Kind == CAPI.LLVMTypeKind.Pointer) {
                    if (destTy.PointerAddressSpace != srcTy.PointerAddressSpace)
                        return CAPI.LLVMOpcode.AddrSpaceCast;
                    return CAPI.LLVMOpcode.BitCast; // ptr -> ptr
                } else if (srcTy.Kind == CAPI.LLVMTypeKind.Integer) {
                    return CAPI.LLVMOpcode.IntToPtr; // int -> ptr
                }

                throw new InvalidCastException("Casting pointer to other than pointer or int");
            } else if (destTy.Kind == CAPI.LLVMTypeKind.X86_MMX) {
                if (srcTy.Kind == CAPI.LLVMTypeKind.Vector) {
                    if (destBits != srcBits)
                        throw new InvalidCastException("Casting vector of wrong width to X86_MMX");
                    return CAPI.LLVMOpcode.BitCast; // 64-bit vector to MMX
                }

                throw new InvalidCastException("Illegal cast to X86_MMX");
            }

            throw new InvalidCastException("Casting to type that is not first-class");
        }
    }
}
