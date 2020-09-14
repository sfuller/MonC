namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class LogicalOrBinOpLeaf : BinaryOperationLeaf
    {
        public LogicalOrBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitLogicalOrBinOp(this);
        }
    }
}
