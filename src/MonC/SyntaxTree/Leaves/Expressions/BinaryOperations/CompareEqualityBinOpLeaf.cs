namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareEqualityBinOpLeaf : BinaryOperationLeaf
    {
        public CompareEqualityBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareEqualityBinOp(this);
        }
    }
}
