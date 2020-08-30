using System;

namespace MonC.LLVM
{
    public sealed class Builder : IDisposable
    {
        private CAPI.LLVMBuilderRef _builder;

        internal Builder(CAPI.LLVMContextRef context)
        {
            _builder = CAPI.LLVMCreateBuilderInContext(context);
        }

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

        ~Builder()
        {
            DoDispose();
        }

        public void Position(BasicBlock block, Value instr)
        {
            CAPI.LLVMPositionBuilder(_builder, block, instr);
        }

        public void PositionBefore(Value instr)
        {
            CAPI.LLVMPositionBuilderBefore(_builder, instr);
        }

        public void PositionAtEnd(BasicBlock block)
        {
            CAPI.LLVMPositionBuilderAtEnd(_builder, block);
        }

        public BasicBlock GetInsertBlock()
        {
            return new BasicBlock(CAPI.LLVMGetInsertBlock(_builder));
        }

        public void ClearInsertionPosition()
        {
            CAPI.LLVMClearInsertionPosition(_builder);
        }

        public void Insert(Value instr)
        {
            CAPI.LLVMInsertIntoBuilder(_builder, instr);
        }

        public void InsertWithName(Value instr, string name)
        {
            CAPI.LLVMInsertIntoBuilderWithName(_builder, instr, name);
        }

        public Value BuildRetVoid() => new Value(CAPI.LLVMBuildRetVoid(_builder));
        public Value BuildRet(Value value) => new Value(CAPI.LLVMBuildRet(_builder, value));

        public Value BuildAggregateRet(Value[] retVals) =>
            new Value(CAPI.LLVMBuildAggregateRet(_builder, Array.ConvertAll(retVals, val => (CAPI.LLVMValueRef) val)));

        public Value BuildBr(BasicBlock dest) => new Value(CAPI.LLVMBuildBr(_builder, dest));

        public Value BuildCondBr(Value ifVal, BasicBlock thenBlock, BasicBlock elseBlock) =>
            new Value(CAPI.LLVMBuildCondBr(_builder, ifVal, thenBlock, elseBlock));

        public Value BuildSwitch(Value val, BasicBlock elseBlock, uint numCases) =>
            new Value(CAPI.LLVMBuildSwitch(_builder, val, elseBlock, numCases));

        public Value BuildIndirectBr(Value addr, uint numDests) =>
            new Value(CAPI.LLVMBuildIndirectBr(_builder, addr, numDests));

        public Value BuildAdd(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildAdd(_builder, lhs, rhs, name));

        public Value BuildNSWAdd(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNSWAdd(_builder, lhs, rhs, name));

        public Value BuildNUWAdd(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNUWAdd(_builder, lhs, rhs, name));

        public Value BuildFAdd(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildFAdd(_builder, lhs, rhs, name));

        public Value BuildSub(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildSub(_builder, lhs, rhs, name));

        public Value BuildNSWSub(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNSWSub(_builder, lhs, rhs, name));

        public Value BuildNUWSub(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNUWSub(_builder, lhs, rhs, name));

        public Value BuildFSub(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildFSub(_builder, lhs, rhs, name));

        public Value BuildMul(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildMul(_builder, lhs, rhs, name));

        public Value BuildNSWMul(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNSWMul(_builder, lhs, rhs, name));

        public Value BuildNUWMul(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildNUWMul(_builder, lhs, rhs, name));

        public Value BuildFMul(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildFMul(_builder, lhs, rhs, name));

        public Value BuildUDiv(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildUDiv(_builder, lhs, rhs, name));

        public Value BuildExactUDiv(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildExactUDiv(_builder, lhs, rhs, name));

        public Value BuildSDiv(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildSDiv(_builder, lhs, rhs, name));

        public Value BuildExactSDiv(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildExactSDiv(_builder, lhs, rhs, name));

        public Value BuildFDiv(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildFDiv(_builder, lhs, rhs, name));

        public Value BuildURem(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildURem(_builder, lhs, rhs, name));

        public Value BuildSRem(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildSRem(_builder, lhs, rhs, name));

        public Value BuildFRem(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildFRem(_builder, lhs, rhs, name));

        public Value BuildShl(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildShl(_builder, lhs, rhs, name));

        public Value BuildLShr(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildLShr(_builder, lhs, rhs, name));

        public Value BuildAShr(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildAShr(_builder, lhs, rhs, name));

        public Value BuildAnd(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildAnd(_builder, lhs, rhs, name));

        public Value BuildOr(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildOr(_builder, lhs, rhs, name));

        public Value BuildXor(Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildXor(_builder, lhs, rhs, name));

        public Value BuildBinOp(CAPI.LLVMOpcode op, Value lhs, Value rhs, string name = "") =>
            new Value(CAPI.LLVMBuildBinOp(_builder, op, lhs, rhs, name));

        public Value BuildNeg(Value v, string name = "") =>
            new Value(CAPI.LLVMBuildNeg(_builder, v, name));

        public Value BuildNSWNeg(Value v, string name = "") =>
            new Value(CAPI.LLVMBuildNSWNeg(_builder, v, name));

        public Value BuildNUWNeg(Value v, string name = "") =>
            new Value(CAPI.LLVMBuildNUWNeg(_builder, v, name));

        public Value BuildFNeg(Value v, string name = "") =>
            new Value(CAPI.LLVMBuildFNeg(_builder, v, name));

        public Value BuildNot(Value v, string name = "") =>
            new Value(CAPI.LLVMBuildNot(_builder, v, name));
    }
}