namespace MonC.SyntaxTree.Nodes.Expressions
{
    public abstract class BinaryOperationNode : ExpressionNode, IBinaryOperationNode
    {
        public IExpressionNode LHS { get; set; }
        public IExpressionNode RHS { get; set; }

        protected BinaryOperationNode(IExpressionNode lhs, IExpressionNode rhs)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitBinaryOperation(this);
        }

        public abstract void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor);
    }
}
