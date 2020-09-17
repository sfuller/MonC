namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class SubtractBinOpNode : BinaryOperationNode
    {
        public SubtractBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitSubtractBinOp(this);
        }
    }
}
