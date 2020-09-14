using MonC.SyntaxTree.Leaves.Expressions;

namespace MonC.SyntaxTree.Leaves
{
    public interface IExpressionVisitor
    {
        void VisitVoid(VoidExpression leaf);

        void VisitNumericLiteral(NumericLiteralLeaf leaf);
        void VisitStringLiteral(StringLiteralLeaf leaf);
        void VisitEnumValue(EnumValueLeaf leaf);
        void VisitVariable(VariableLeaf leaf);

        void VisitUnaryOperation(IUnaryOperationLeaf leaf);
        void VisitBinaryOperation(IBinaryOperationLeaf leaf);

        void VisitFunctionCall(FunctionCallLeaf leaf);
        void VisitAssignment(AssignmentLeaf leaf);

        void VisitUnknown(IExpressionLeaf leaf);
    }
}
