namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class DivideBinOpNode : BinaryOperationNode
    {
        public DivideBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitDivideBinOp(this);
        }
    }
}
