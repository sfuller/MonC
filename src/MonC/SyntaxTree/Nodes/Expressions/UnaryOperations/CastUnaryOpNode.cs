using MonC.SyntaxTree.Nodes.Specifiers;

namespace MonC.SyntaxTree.Nodes.Expressions.UnaryOperations
{
    public class CastUnaryOpNode : UnaryOperationNode
    {
        public ITypeSpecifierNode ToType;
        public bool Reinterpret;

        public CastUnaryOpNode(ITypeSpecifierNode toType, IExpressionNode rhs, bool reinterpret) : base(rhs)
        {
            ToType = toType;
            Reinterpret = reinterpret;
        }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitCastUnaryOp(this);
        }
    }
}
