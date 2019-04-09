namespace MonC.Bytecode
{
    public class CompareLTInstruction : IInstruction
    {
        public Opcode Op => Opcode.COMPARE_LT;
    }
}