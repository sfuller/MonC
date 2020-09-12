namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class MultiplyBinOpLeaf : BinaryOperationLeaf
    {
        public MultiplyBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitMultiplyBinOp(this);
        }
    }
}
