namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareGtBinOpNode : BinaryOperationNode
    {
        public CompareGtBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareGTBinOp(this);
        }
    }
}
