namespace MonC.Bytecode
{
    public class StoreInstruction : IInstruction
    {
        public Opcode Op => Opcode.STORE;
    }
}