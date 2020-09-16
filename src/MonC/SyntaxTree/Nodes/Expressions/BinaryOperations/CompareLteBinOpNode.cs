namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareLteBinOpNode : BinaryOperationNode
    {
        public CompareLteBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareLTEBinOp(this);
        }
    }
}
