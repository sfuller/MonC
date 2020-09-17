namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareGteBinOpNode : BinaryOperationNode
    {
        public CompareGteBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareGTEBinOp(this);
        }
    }
}
