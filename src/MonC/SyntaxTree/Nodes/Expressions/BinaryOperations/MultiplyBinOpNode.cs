namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class MultiplyBinOpNode : BinaryOperationNode
    {
        public MultiplyBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitMultiplyBinOp(this);
        }
    }
}
