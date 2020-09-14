namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareLTEBinOpLeaf : BinaryOperationLeaf
    {
        public CompareLTEBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareLTEBinOp(this);
        }
    }
}
