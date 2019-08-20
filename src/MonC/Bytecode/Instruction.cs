
namespace MonC.Bytecode
{
    public struct Instruction
    {
        public OpCode Op;
        public int ImmediateValue;

        public Instruction(OpCode op)
        {
            Op = op;
            ImmediateValue = 0;
        }

        public Instruction(OpCode op, int immediate)
        {
            Op = op;
            ImmediateValue = immediate;
        }
    }
}