namespace MonC.Bytecode
{
    public class CompareLTEInstruction : IInstruction
    {
        public Opcode Op => Opcode.COMPARE_LTE;
    }
}