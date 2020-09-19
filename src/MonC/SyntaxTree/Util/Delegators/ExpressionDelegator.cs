using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class ExpressionDelegator : NoOpExpressionVisitor
    {
        public IExpressionVisitor? TopLevelVisitor;
        public IBinaryOperationVisitor? BinaryOperationVisitor;
        public IUnaryOperationVisitor? UnaryOperationVisitor;

        protected override void VisitDefaultExpression(IExpressionNode node)
        {
            if (TopLevelVisitor != null) {
                node.AcceptExpressionVisitor(TopLevelVisitor);
            }
        }

        public override void VisitBinaryOperation(IBinaryOperationNode node)
        {
            if (BinaryOperationVisitor != null) {
                node.AcceptBinaryOperationVisitor(BinaryOperationVisitor);
            }
        }

        public override void VisitUnaryOperation(IUnaryOperationNode node)
        {
            if (UnaryOperationVisitor != null) {
                node.AcceptUnaryOperationVisitor(UnaryOperationVisitor);
            }
        }
    }
}
