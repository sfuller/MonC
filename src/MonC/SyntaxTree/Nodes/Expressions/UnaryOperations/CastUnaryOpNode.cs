namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class CastUnaryOpNode : UnaryOperationNode
    {
        public TypeSpecifier ToType;

        public CastUnaryOpNode(TypeSpecifier toType, IExpressionNode rhs) : base(rhs)
        {
            ToType = toType;
        }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitCastUnaryOp(this);
        }
    }
}
