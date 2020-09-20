namespace MonC.SyntaxTree.Nodes
{
    public abstract class ExpressionNode : IExpressionNode
    {
        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitExpression(this);
        }

        public abstract void AcceptExpressionVisitor(IExpressionVisitor visitor);
    }
}
