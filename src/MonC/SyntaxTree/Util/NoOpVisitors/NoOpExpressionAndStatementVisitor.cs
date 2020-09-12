using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Util.NoOpVisitors
{
    public class NoOpExpressionAndStatementVisitor : NoOpExpressionVisitor, IStatementVisitor
    {
        public virtual void VisitDeclaration(DeclarationLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitBreak(BreakLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitContinue(ContinueLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitReturn(ReturnLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitIfElse(IfElseLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitFor(ForLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public virtual void VisitWhile(WhileLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            VisitDefaultStatement(leaf);
        }

        protected virtual void VisitDefaultStatement(IStatementLeaf leaf)
        {
        }
    }
}
