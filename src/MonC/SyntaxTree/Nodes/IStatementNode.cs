namespace MonC.SyntaxTree.Nodes
{
    public interface IStatementNode : ISyntaxTreeNode
    {
        void AcceptStatementVisitor(IStatementVisitor visitor);
    }
}
