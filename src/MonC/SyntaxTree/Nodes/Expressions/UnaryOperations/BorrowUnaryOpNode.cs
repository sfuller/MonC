namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class BorrowUnaryOpNode : UnaryOperationNode
    {
        public BorrowUnaryOpNode(IExpressionNode rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitBorrowUnaryOp(this);
        }
    }
}
