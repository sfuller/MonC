
namespace MonC.Bytecode
{
    public class PushDataInstruction : IInstruction
    {
        public readonly int Offset;

        public PushDataInstruction(int offset)
        {
            Offset = offset;
        }
        
        public Opcode Op => Opcode.PUSH_DATA;
    }
}