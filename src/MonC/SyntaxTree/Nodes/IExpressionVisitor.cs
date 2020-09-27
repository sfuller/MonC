using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Nodes
{
    public interface IExpressionVisitor
    {
        void VisitBasicExpression(IBasicExpression node);
        void VisitUnaryOperation(IUnaryOperationNode node);
        void VisitBinaryOperation(IBinaryOperationNode node);

        void VisitUnknown(IExpressionNode node);
    }
}
