namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class LogicalNotUnaryOpNode : UnaryOperationNode
    {
        public LogicalNotUnaryOpNode(IExpressionNode rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitLogicalNotUnaryOp(this);
        }
    }
}
