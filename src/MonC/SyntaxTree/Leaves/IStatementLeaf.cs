namespace MonC.SyntaxTree.Leaves
{
    public interface IStatementLeaf : ISyntaxTreeLeaf
    {
        void AcceptStatementVisitor(IStatementVisitor visitor);
    }
}
