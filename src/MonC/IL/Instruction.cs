
namespace MonC.IL
{
    public struct Instruction
    {
        public OpCode Op;
        public int ImmediateValue;
        public int SizeValue;

        public Instruction(OpCode op)
        {
            Op = op;
            ImmediateValue = 0;
            SizeValue = 0;
        }

        public Instruction(OpCode op, int immediate)
        {
            Op = op;
            ImmediateValue = immediate;
            SizeValue = 0;
        }

        public Instruction(OpCode op, int immediate, int size)
        {
            Op = op;
            ImmediateValue = immediate;
            SizeValue = size;
        }
    }
}
