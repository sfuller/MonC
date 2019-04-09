namespace MonC.Bytecode
{
    public class CompareInstruction : IInstruction
    {
        public Opcode Op {
            get { return Opcode.COMPARE; }
        }
    }
}