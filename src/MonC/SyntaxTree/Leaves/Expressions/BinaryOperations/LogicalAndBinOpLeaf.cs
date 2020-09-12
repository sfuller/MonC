namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class LogicalAndBinOpLeaf : BinaryOperationLeaf
    {
        public LogicalAndBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitLogicalAndBinOp(this);
        }
    }
}
