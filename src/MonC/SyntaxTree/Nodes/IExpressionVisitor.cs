using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Nodes
{
    public interface IExpressionVisitor
    {
        void VisitVoid(VoidExpressionNode node);

        void VisitNumericLiteral(NumericLiteralNode node);
        void VisitStringLiteral(StringLiteralNode node);
        void VisitEnumValue(EnumValueNode node);
        void VisitVariable(VariableNode node);

        void VisitUnaryOperation(IUnaryOperationNode node);
        void VisitBinaryOperation(IBinaryOperationNode node);

        void VisitFunctionCall(FunctionCallNode node);
        void VisitAssignment(AssignmentNode node);

        void VisitUnknown(IExpressionNode node);
    }
}
