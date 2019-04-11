namespace MonC.Bytecode
{
    public class PushLocalInstruction : IInstruction
    {
        public Opcode Op => Opcode.PUSH_LOCAL;
    }
}