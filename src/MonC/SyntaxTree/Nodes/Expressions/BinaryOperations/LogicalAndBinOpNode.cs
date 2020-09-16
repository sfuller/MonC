namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class LogicalAndBinOpNode : BinaryOperationNode
    {
        public LogicalAndBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitLogicalAndBinOp(this);
        }
    }
}
