using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IUnaryOperationVisitor
    {
        void VisitNegateUnaryOp(NegateUnaryOpNode node);
        void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node);
        void VisitCastUnaryOp(CastUnaryOpNode node);
    }
}
