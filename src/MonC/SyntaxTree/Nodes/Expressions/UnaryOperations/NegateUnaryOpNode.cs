namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class NegateUnaryOpNode : UnaryOperationNode
    {
        public NegateUnaryOpNode(IExpressionNode rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitNegateUnaryOp(this);
        }
    }
}
