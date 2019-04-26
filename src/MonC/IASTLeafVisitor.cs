using MonC.SyntaxTree;

namespace MonC
{
    public interface IASTLeafVisitor
    {
        void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf);
        void VisitBody(BodyLeaf leaf);
        void VisitDeclaration(DeclarationLeaf leaf);
        void VisitFor(ForLeaf leaf);
        void VisitFunctionDefinition(FunctionDefinitionLeaf leaf);
        void VisitFunctionCall(FunctionCallLeaf leaf);
        void VisitIdentifier(IdentifierLeaf leaf);
        void VisitIfElse(IfElseLeaf leaf);
        void VisitNumericLiteral(NumericLiteralLeaf leaf);
        void VisitStringLiteral(StringLiteralLeaf leaf);
        void VisitWhile(WhileLeaf leaf);
        void VisitBreak(BreakLeaf leaf);
        void VisitReturn(ReturnLeaf leaf);
    }
}