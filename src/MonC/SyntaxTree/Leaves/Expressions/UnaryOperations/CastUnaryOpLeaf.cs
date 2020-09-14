namespace MonC.SyntaxTree.Leaves.Expressions.UnaryOperations
{
    public class CastUnaryOpLeaf : UnaryOperationLeaf
    {
        public TypeSpecifier ToType;

        public CastUnaryOpLeaf(TypeSpecifier toType, IExpressionLeaf rhs) : base(rhs)
        {
            ToType = toType;
        }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitCastUnaryOp(this);
        }
    }
}
