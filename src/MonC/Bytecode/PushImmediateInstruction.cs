namespace MonC.Bytecode
{
    public class PushImmediateInstruction : IInstruction
    {
        public readonly int Value;

        public PushImmediateInstruction(int value)
        {
            Value = value;
        }
        
        public Opcode Op => Opcode.PUSH_IMMEDIATE;
    }
}