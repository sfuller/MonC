using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.GenericDelegators
{
    public class GenericExpressionDelegator : IExpressionVisitor
    {
        private readonly IVisitor<IExpressionNode> _visitor;

        public GenericExpressionDelegator(IVisitor<IExpressionNode> visitor)
        {
            _visitor = visitor;
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitVariable(VariableNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            _visitor.Visit(node);
        }
    }
}
