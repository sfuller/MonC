namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareGTEBinOpLeaf : BinaryOperationLeaf
    {
        public CompareGTEBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareGTEBinOp(this);
        }
    }
}
