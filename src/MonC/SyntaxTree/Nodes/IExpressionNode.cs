namespace MonC.SyntaxTree.Nodes
{
    public interface IExpressionNode : ISyntaxTreeNode
    {
        void AcceptExpressionVisitor(IExpressionVisitor visitor);
    }
}
