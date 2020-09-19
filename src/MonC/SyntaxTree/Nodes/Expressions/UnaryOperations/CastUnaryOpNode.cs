using MonC.SyntaxTree.Nodes.Specifiers;

namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class CastUnaryOpNode : UnaryOperationNode
    {
        public ITypeSpecifierNode ToType;

        public CastUnaryOpNode(ITypeSpecifierNode toType, IExpressionNode rhs) : base(rhs)
        {
            ToType = toType;
        }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitCastUnaryOp(this);
        }
    }
}
