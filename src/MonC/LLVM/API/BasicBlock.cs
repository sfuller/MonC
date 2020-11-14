using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public struct BasicBlock
    {
        private LLVMBasicBlockRef _basicBlock;
        public bool IsValid => _basicBlock.Handle != IntPtr.Zero;

        public static BasicBlock Null => new BasicBlock();

        internal BasicBlock(LLVMBasicBlockRef basicBlock) => _basicBlock = basicBlock;

        public static implicit operator LLVMBasicBlockRef(BasicBlock basicBlock) => basicBlock._basicBlock;
        public static implicit operator BasicBlock(LLVMBasicBlockRef basicBlock) => new BasicBlock(basicBlock);

        public Value FirstInstruction => _basicBlock.FirstInstruction;
        public Value LastInstruction => _basicBlock.LastInstruction;
    }
}
