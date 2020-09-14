namespace MonC.SyntaxTree.Leaves.Expressions
{
    public abstract class UnaryOperationLeaf : IUnaryOperationLeaf
    {
        public IExpressionLeaf RHS { get; set; }

        protected UnaryOperationLeaf(IExpressionLeaf rhs)
        {
            RHS = rhs;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnaryOperation(this);
        }

        public abstract void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor);
    }
}
