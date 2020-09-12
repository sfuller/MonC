namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class DivideBinOpLeaf : BinaryOperationLeaf
    {
        public DivideBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitDivideBinOp(this);
        }
    }
}
