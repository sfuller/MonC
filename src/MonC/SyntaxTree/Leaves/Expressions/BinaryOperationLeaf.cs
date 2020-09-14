namespace MonC.SyntaxTree.Leaves.Expressions
{
    public abstract class BinaryOperationLeaf : IBinaryOperationLeaf
    {
        public IExpressionLeaf LHS { get; set; }
        public IExpressionLeaf RHS { get; set; }

        //public Token Op;

        protected BinaryOperationLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs)
        {
            LHS = lhs;
            RHS = rhs;
            // Op = op;
        }

        public abstract void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor);

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitBinaryOperation(this);
        }
    }
}
