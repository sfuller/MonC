namespace MonC.SyntaxTree.Leaves.Expressions.UnaryOperations
{
    public class NegateUnaryOpLeaf : UnaryOperationLeaf
    {
        public NegateUnaryOpLeaf(IExpressionLeaf rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitNegateUnaryOp(this);
        }
    }
}
