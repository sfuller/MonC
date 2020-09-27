using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class ExpressionDelegator : IExpressionVisitor
    {
        public IBasicExpressionVisitor? BasicVisitor;
        public IBinaryOperationVisitor? BinaryOperationVisitor;
        public IUnaryOperationVisitor? UnaryOperationVisitor;
        public IVisitor<IExpressionNode>? UnknownVisitor;

        public void VisitBasicExpression(IBasicExpression node)
        {
            if (BasicVisitor != null) {
                node.AcceptBasicExpressionVisitor(BasicVisitor);
            }
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            if (BinaryOperationVisitor != null) {
                node.AcceptBinaryOperationVisitor(BinaryOperationVisitor);
            }
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            if (UnaryOperationVisitor != null) {
                node.AcceptUnaryOperationVisitor(UnaryOperationVisitor);
            }
        }

        public void VisitUnknown(IExpressionNode node)
        {
            UnknownVisitor?.Visit(node);
        }
    }
}
