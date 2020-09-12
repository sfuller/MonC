using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Leaves
{
    public interface IStatementVisitor
    {
        void VisitDeclaration(DeclarationLeaf leaf);
        void VisitBreak(BreakLeaf leaf);
        void VisitContinue(ContinueLeaf leaf);
        void VisitReturn(ReturnLeaf leaf);

        void VisitIfElse(IfElseLeaf leaf);
        void VisitFor(ForLeaf leaf);
        void VisitWhile(WhileLeaf leaf);

        void VisitExpressionStatement(ExpressionStatementLeaf leaf);
    }
}
