namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IBasicExpression : IExpressionNode
    {
        void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor);
    }
}
