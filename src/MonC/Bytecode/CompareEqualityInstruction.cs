namespace MonC.Bytecode
{
    public class CompareEqualityInstruction : IInstruction
    {
        public Opcode Op => Opcode.COMPARE_EQUAL;
    }
}