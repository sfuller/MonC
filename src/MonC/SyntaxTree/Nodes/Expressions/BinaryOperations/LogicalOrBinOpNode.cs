namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class LogicalOrBinOpNode : BinaryOperationNode
    {
        public LogicalOrBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitLogicalOrBinOp(this);
        }
    }
}
