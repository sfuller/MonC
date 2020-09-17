namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class AddBinOpNode : BinaryOperationNode
    {
        public AddBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitAddBinOp(this);
        }
    }
}
