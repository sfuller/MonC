namespace MonC.LLVM
{
    public struct BasicBlock
    {
        private CAPI.LLVMBasicBlockRef _basicBlock;
        public bool IsValid => _basicBlock.IsValid;

        public static BasicBlock Null => new BasicBlock();

        internal BasicBlock(CAPI.LLVMBasicBlockRef basicBlock) => _basicBlock = basicBlock;

        public static implicit operator CAPI.LLVMBasicBlockRef(BasicBlock basicBlock) => basicBlock._basicBlock;
        public static implicit operator BasicBlock(CAPI.LLVMBasicBlockRef basicBlock) => new BasicBlock(basicBlock);

        public Value FirstInstruction => CAPI.LLVMGetFirstInstruction(_basicBlock);
        public Value LastInstruction => CAPI.LLVMGetLastInstruction(_basicBlock);
    }
}
