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

        public BasicBlock GetInsertBlock() => CAPI.LLVMGetInsertBlock(_builder);

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

        public Value BuildICmp(CAPI.LLVMIntPredicate op, Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildICmp(_builder, op, lhs, rhs, name);

        public Value BuildFCmp(CAPI.LLVMRealPredicate op, Value lhs, Value rhs, string name = "") =>
            CAPI.LLVMBuildFCmp(_builder, op, lhs, rhs, name);

        public Value BuildPhi(Type ty, string name = "") => CAPI.LLVMBuildPhi(_builder, ty, name);

        public Value BuildAlloca(Type ty, string name = "") => CAPI.LLVMBuildAlloca(_builder, ty, name);

        public Value BuildArrayAlloca(Type ty, Value val, string name = "") =>
            CAPI.LLVMBuildArrayAlloca(_builder, ty, val, name);

        public Value BuildLoad(Type ty, Value ptr, string name = "") => CAPI.LLVMBuildLoad2(_builder, ty, ptr, name);

        public Value BuildStore(Value val, Value ptr) => CAPI.LLVMBuildStore(_builder, val, ptr);

        public void SetCurrentDebugLocation(Metadata loc) => CAPI.LLVMSetCurrentDebugLocation2(_builder, loc);
    }
}