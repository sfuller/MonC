using JetBrains.Annotations;

namespace MonC.SyntaxTree.Leaves.Expressions.UnaryOperations
{
    public class NegateUnaryOpLeaf : UnaryOperationLeaf
    {
        public NegateUnaryOpLeaf([NotNull] IExpressionLeaf rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitNegateUnaryOp(this);
        }
    }
}
