using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class ExpressionChildrenVisitor : IExpressionVisitor, IUnaryOperationVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;

        public ExpressionChildrenVisitor(ISyntaxTreeVisitor visitor)
        {
            _visitor = visitor;
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitVariable(VariableNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            node.AcceptUnaryOperationVisitor(this);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            _visitor.VisitExpression(node);

            // NOTE: We may want to make this recursion of LHS and RHS optional, in case the outer visitor uses a
            // IBinaryOperationVisitor.
            node.LHS.AcceptExpressionVisitor(this);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            _visitor.VisitExpression(node);
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                IExpressionNode argument = node.GetArgument(i);
                argument.AcceptExpressionVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentNode node)
        {
            _visitor.VisitExpression(node);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node)
        {
            _visitor.VisitExpression(node);
        }

        public void VisitCastUnaryOp(CastUnaryOpNode node)
        {
            _visitor.VisitExpression(node);
            _visitor.VisitSpecifier(node.ToType);
        }
    }
}
