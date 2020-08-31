namespace MonC.LLVM
{
    public struct BasicBlock
    {
        private CAPI.LLVMBasicBlockRef _basicBlock;
        public bool IsValid => _basicBlock.IsValid;

        public static BasicBlock Null => new BasicBlock();

        internal BasicBlock(CAPI.LLVMBasicBlockRef basicBlock)
        {
            _basicBlock = basicBlock;
        }

        public static implicit operator CAPI.LLVMBasicBlockRef(BasicBlock basicBlock) => basicBlock._basicBlock;

        public Value FirstInstruction => new Value(CAPI.LLVMGetFirstInstruction(_basicBlock));
        public Value LastInstruction => new Value(CAPI.LLVMGetLastInstruction(_basicBlock));
    }
}