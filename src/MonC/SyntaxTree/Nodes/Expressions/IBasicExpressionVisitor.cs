namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IBasicExpressionVisitor
    {
        void VisitVoid(VoidExpressionNode node);

        void VisitNumericLiteral(NumericLiteralNode node);
        void VisitStringLiteral(StringLiteralNode node);
        void VisitEnumValue(EnumValueNode node);
        void VisitVariable(VariableNode node);

        void VisitFunctionCall(FunctionCallNode node);
        void VisitAssignment(AssignmentNode node);
        void VisitAccess(AccessNode node);
    }
}
