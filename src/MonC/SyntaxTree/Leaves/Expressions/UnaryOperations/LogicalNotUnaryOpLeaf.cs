using JetBrains.Annotations;

namespace MonC.SyntaxTree.Leaves.Expressions.UnaryOperations
{
    public class LogicalNotUnaryOpLeaf : UnaryOperationLeaf
    {
        public LogicalNotUnaryOpLeaf([NotNull] IExpressionLeaf rhs) : base(rhs) { }

        public override void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor)
        {
            visitor.VisitLogicalNotUnaryOp(this);
        }
    }
}
