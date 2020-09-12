namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class SubtractBinOpLeaf : BinaryOperationLeaf
    {
        public SubtractBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitSubtractBinOp(this);
        }
    }
}
