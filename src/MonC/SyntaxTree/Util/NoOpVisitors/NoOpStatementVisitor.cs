using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.NoOpVisitors
{
    public class NoOpStatementVisitor : IStatementVisitor
    {
        public void VisitBody(BodyNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitDeclaration(DeclarationNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitBreak(BreakNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitContinue(ContinueNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitReturn(ReturnNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitIfElse(IfElseNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitFor(ForNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitWhile(WhileNode node)
        {
            VisitDefaultStatement(node);
        }

        public virtual void VisitExpressionStatement(ExpressionStatementNode node)
        {
            VisitDefaultStatement(node);
        }

        protected virtual void VisitDefaultStatement(IStatementNode node)
        {
        }
    }
}
