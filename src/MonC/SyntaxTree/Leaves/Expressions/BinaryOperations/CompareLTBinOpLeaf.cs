namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareLTBinOpLeaf : BinaryOperationLeaf
    {
        public CompareLTBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareLTBinOp(this);
        }
    }
}
