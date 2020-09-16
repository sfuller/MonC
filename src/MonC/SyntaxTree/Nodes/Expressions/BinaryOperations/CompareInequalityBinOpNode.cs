namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class CompareInequalityBinOpNode : BinaryOperationNode
    {
        public CompareInequalityBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitCompareInequalityBinOp(this);
        }
    }
}
