namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class DereferenceUnaryOpNode : UnaryOperationNode
    {
        public DereferenceUnaryOpNode(IExpressionNode rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitDereferenceUnaryOp(this);
        }
    }
}
