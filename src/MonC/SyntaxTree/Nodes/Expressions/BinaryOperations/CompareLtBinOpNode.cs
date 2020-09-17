namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareLtBinOpNode : BinaryOperationNode
    {
        public CompareLtBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareLTBinOp(this);
        }
    }
}
