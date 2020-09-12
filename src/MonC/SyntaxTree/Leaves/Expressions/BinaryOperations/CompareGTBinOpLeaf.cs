namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareGTBinOpLeaf : BinaryOperationLeaf
    {
        public CompareGTBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareGTBinOp(this);
        }
    }
}
