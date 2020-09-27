namespace MonC.SyntaxTree.Nodes.Expressions
{
    public abstract class BasicExpression : ExpressionNode, IBasicExpression
    {
        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitBasicExpression(this);
        }

        public abstract void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor);
    }
}
