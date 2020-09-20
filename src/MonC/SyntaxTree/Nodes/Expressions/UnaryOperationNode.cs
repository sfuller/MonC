namespace MonC.SyntaxTree.Nodes.Expressions
{
    public abstract class UnaryOperationNode : IUnaryOperationNode
    {
        public IExpressionNode RHS { get; set; }

        protected UnaryOperationNode(IExpressionNode rhs)
        {
            RHS = rhs;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitExpression(this);
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnaryOperation(this);
        }

        public abstract void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor);
    }
}
