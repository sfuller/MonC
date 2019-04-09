namespace MonC
{
    public interface IASTLeaf
    {
        void Accept(IASTLeafVisitor visitor);
    }
}