using MonC.SyntaxTree;

namespace MonC
{
    public interface IASTLeafVisitor
    {
        void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf);
        void VisitDeclaration(DeclarationLeaf leaf);
        void VisitFor(ForLeaf leaf);
        void VisitFunction(FunctionLeaf leaf);
        void VisitIdentifier(IdentifierLeaf leaf);
        void VisitIfElse(IfElseLeaf leaf);
        void VisitNumericLiteral(NumericLiteralLeaf leaf);
        void VisitPlaceholder(PlaceholderLeaf leaf);
        void VisitStringLiteral(StringLIteralLeaf leaf);
        void VisitWhile(WhileLeaf leaf);
    }
}