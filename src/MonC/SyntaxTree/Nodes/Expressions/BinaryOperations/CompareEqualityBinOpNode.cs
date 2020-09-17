namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareEqualityBinOpNode : BinaryOperationNode
    {
        public CompareEqualityBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareEqualityBinOp(this);
        }
    }
}
