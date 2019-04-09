namespace MonC.Bytecode
{
    public class BranchInstruction : IInstruction
    {
        public readonly int Offset;

        public BranchInstruction(int offset)
        {
            Offset = offset;
        }
        
        public Opcode Op => Opcode.BRANCH;
    }
}