namespace MonC.SyntaxTree.Leaves
{
    public interface IExpressionLeaf : ISyntaxTreeLeaf
    {
        void AcceptExpressionVisitor(IExpressionVisitor visitor);
    }
}
