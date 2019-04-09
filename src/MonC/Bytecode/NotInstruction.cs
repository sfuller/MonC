namespace MonC.Bytecode
{
    public class NotInstruction : IInstruction
    {
        public Opcode Op => Opcode.NOT;
    }
}