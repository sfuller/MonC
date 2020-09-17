using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

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

        public void VisitBody(BodyNode node)
        {
            _statementVisitor.VisitBody(node);
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _statementVisitor.VisitDeclaration(node);
            node.Assignment.AcceptExpressionVisitor(_expressionVisitor);
        }

        public void VisitBreak(BreakNode node)
        {
            _statementVisitor.VisitBreak(node);
        }

        public void VisitContinue(ContinueNode node)
        {
            _statementVisitor.VisitContinue(node);
        }

        public void VisitReturn(ReturnNode node)
        {
            _statementVisitor.VisitReturn(node);
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
        }

        public void VisitIfElse(IfElseNode node)
        {
            _statementVisitor.VisitIfElse(node);
            node.Condition.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            _statementVisitor.VisitFor(node);
            node.Declaration.AcceptStatementVisitor(this);
            node.Condition.AcceptExpressionVisitor(_expressionVisitor);
            node.Update.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(node.Body);
        }

        public void VisitWhile(WhileNode node)
        {
            _statementVisitor.VisitWhile(node);
            node.Condition.AcceptExpressionVisitor(_expressionVisitor);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            _statementVisitor.VisitExpressionStatement(node);
            node.Expression.AcceptExpressionVisitor(_expressionVisitor);
        }

    }
}
