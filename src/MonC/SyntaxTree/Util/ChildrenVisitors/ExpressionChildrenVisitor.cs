using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class ExpressionChildrenVisitor : IExpressionVisitor
    {
        public IExpressionVisitor Visitor;

        public ExpressionChildrenVisitor(IExpressionVisitor visitor)
        {
            Visitor = visitor;
        }

        public ExpressionChildrenVisitor SetVisitor(IExpressionVisitor visitor)
        {
            Visitor = visitor;
            return this;
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            Visitor.VisitVoid(node);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            Visitor.VisitNumericLiteral(node);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            Visitor.VisitStringLiteral(node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            Visitor.VisitEnumValue(node);
        }

        public void VisitVariable(VariableNode node)
        {
            Visitor.VisitVariable(node);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            Visitor.VisitUnaryOperation(node);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            Visitor.VisitBinaryOperation(node);

            // NOTE: We may want to make this recursion of LHS and RHS optional, in case the outer visitor uses a
            // IBinaryOperationVisitor.
            node.LHS.AcceptExpressionVisitor(this);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            Visitor.VisitFunctionCall(node);
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                IExpressionNode argument = node.GetArgument(i);
                argument.AcceptExpressionVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentNode node)
        {
            Visitor.VisitAssignment(node);
            node.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            Visitor.VisitUnknown(node);
        }
    }
}
