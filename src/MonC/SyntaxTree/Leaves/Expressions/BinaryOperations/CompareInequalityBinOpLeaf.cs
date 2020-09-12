namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class CompareInequalityBinOpLeaf : BinaryOperationLeaf
    {
        public CompareInequalityBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareInequalityBinOp(this);
        }
    }
}
