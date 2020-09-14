namespace MonC.SyntaxTree.Leaves.Expressions
{
    public abstract class BinaryOperationLeaf : IBinaryOperationLeaf
    {
        public IExpressionLeaf LHS { get; set; }
        public IExpressionLeaf RHS { get; set; }

        protected BinaryOperationLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitBinaryOperation(this);
        }

        public abstract void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor);
    }
}
