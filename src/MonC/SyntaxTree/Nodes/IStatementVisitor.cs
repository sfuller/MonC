using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes
{
    public interface IStatementVisitor
    {
        void VisitBody(BodyNode node);

        void VisitDeclaration(DeclarationNode node);
        void VisitBreak(BreakNode node);
        void VisitContinue(ContinueNode node);
        void VisitReturn(ReturnNode node);

        void VisitIfElse(IfElseNode node);
        void VisitFor(ForNode node);
        void VisitWhile(WhileNode node);

        void VisitExpressionStatement(ExpressionStatementNode node);
    }
}
