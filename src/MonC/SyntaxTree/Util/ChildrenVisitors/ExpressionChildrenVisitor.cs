using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class ExpressionChildrenVisitor : IExpressionVisitor, IUnaryOperationVisitor, IBasicExpressionVisitor
    {
        private readonly ISyntaxTreeVisitor? _preOrderVisitor;
        private readonly ISyntaxTreeVisitor? _postOrderVisitor;
        private readonly ISyntaxTreeVisitor _childrenVisitor;
        public IVisitor<IExpressionNode>? ExtensionChildrenVisitor;

        public ExpressionChildrenVisitor(ISyntaxTreeVisitor? preOrderVisitor, ISyntaxTreeVisitor? postOrderVisitor, ISyntaxTreeVisitor childrenVisitor)
        {
            _preOrderVisitor = preOrderVisitor;
            _postOrderVisitor = postOrderVisitor;
            _childrenVisitor = childrenVisitor;
        }

        private void VisitPreOrder(ISyntaxTreeNode node)
        {
            if (_preOrderVisitor != null) {
                node.AcceptSyntaxTreeVisitor(_preOrderVisitor);
            }
        }

        private void VisitPostOrder(ISyntaxTreeNode node)
        {
            if (_postOrderVisitor != null) {
                node.AcceptSyntaxTreeVisitor(_postOrderVisitor);
            }
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitVariable(VariableNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitBasicExpression(IBasicExpression node)
        {
            node.AcceptBasicExpressionVisitor(this);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            node.AcceptUnaryOperationVisitor(this);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            VisitPreOrder(node);

            // NOTE: We may want to make this recursion of LHS and RHS optional, in case the outer visitor uses a
            // IBinaryOperationVisitor.
            node.LHS.AcceptExpressionVisitor(this);
            node.RHS.AcceptExpressionVisitor(this);

            VisitPostOrder(node);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            VisitPreOrder(node);

            foreach (IExpressionNode argument in node.Arguments) {
                argument.AcceptExpressionVisitor(this);
            }

            VisitPostOrder(node);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            VisitPreOrder(node);
            node.Rhs.AcceptExpressionVisitor(this);
            VisitPostOrder(node);
        }

        public void VisitAccess(AccessNode node)
        {
            VisitPreOrder(node);
            node.Lhs.AcceptExpressionVisitor(this);
            VisitPostOrder(node);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            ExtensionChildrenVisitor?.Visit(node);
        }

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
            VisitPreOrder(node);
            node.RHS.AcceptExpressionVisitor(this);
            VisitPostOrder(node);
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node)
        {
            VisitPreOrder(node);
            node.RHS.AcceptExpressionVisitor(this);
            VisitPostOrder(node);
        }

        public void VisitCastUnaryOp(CastUnaryOpNode node)
        {
            VisitPreOrder(node);
            node.RHS.AcceptExpressionVisitor(this);
            node.ToType.AcceptSyntaxTreeVisitor(_childrenVisitor);
            VisitPostOrder(node);
        }
    }
}
