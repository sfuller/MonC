using MonC.SyntaxTree.Leaves.Expressions.UnaryOperations;

namespace MonC.SyntaxTree.Leaves.Expressions
{
    public interface IUnaryOperationVisitor
    {
        void VisitNegateUnaryOp(NegateUnaryOpLeaf leaf);
        void VisitLogicalNotUnaryOp(LogicalNotUnaryOpLeaf leaf);
    }
}
