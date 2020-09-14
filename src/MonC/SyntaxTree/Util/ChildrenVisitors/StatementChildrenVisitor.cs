using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class StatementChildrenVisitor : IStatementVisitor
    {
        private readonly IStatementVisitor _statementVisitor;
        private readonly IExpressionVisitor _expressionVisitor;

        public StatementChildrenVisitor(IStatementVisitor statementVisitor, IExpressionVisitor expressionVisitor)
        {
            _statementVisitor = statementVisitor;
            _expressionVisitor = expressionVisitor;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _statementVisitor.VisitDeclaration(leaf);
            leaf.Assignment.AcceptExpressionVisitor(_expressionVisitor);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            _statementVisitor.VisitBreak(leaf);
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            _statementVisitor.VisitContinue(leaf);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            _statementVisitor.VisitReturn(leaf);
            leaf.RHS.AcceptExpressionVisitor(_expressionVisitor);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            _statementVisitor.VisitIfElse(leaf);
            leaf.Condition.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(leaf.IfBody);
            VisitBody(leaf.ElseBody);
        }

        public void VisitFor(ForLeaf leaf)
        {
            _statementVisitor.VisitFor(leaf);
            leaf.Declaration.AcceptStatementVisitor(this);
            leaf.Condition.AcceptExpressionVisitor(_expressionVisitor);
            leaf.Update.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(leaf.Body);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            _statementVisitor.VisitWhile(leaf);
            leaf.Condition.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(leaf.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            _statementVisitor.VisitExpressionStatement(leaf);
            leaf.Expression.AcceptExpressionVisitor(_expressionVisitor);
        }

        private void VisitBody(Body body)
        {
            for (int i = 0, ilen = body.Length; i < ilen; ++i) {
                IStatementLeaf statement = body.GetStatement(i);
                statement.AcceptStatementVisitor(this);
            }
        }

    }
}
