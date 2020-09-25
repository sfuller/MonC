using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class StatementChildrenVisitor : IStatementVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;

        public StatementChildrenVisitor(ISyntaxTreeVisitor visitor)
        {
            _visitor = visitor;
        }

        public void VisitBody(BodyNode node)
        {
            _visitor.VisitStatement(node);
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _visitor.VisitStatement(node);
            _visitor.VisitSpecifier(node.Type);
            _visitor.VisitExpression(node.Assignment);
        }

        public void VisitBreak(BreakNode node)
        {
            _visitor.VisitStatement(node);
        }

        public void VisitContinue(ContinueNode node)
        {
            _visitor.VisitStatement(node);
        }

        public void VisitReturn(ReturnNode node)
        {
            _visitor.VisitStatement(node);
            node.RHS.AcceptSyntaxTreeVisitor(_visitor);
        }

        public void VisitIfElse(IfElseNode node)
        {
            _visitor.VisitStatement(node);
            node.Condition.AcceptSyntaxTreeVisitor(_visitor);
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            _visitor.VisitStatement(node);
            VisitDeclaration(node.Declaration);
            node.Condition.AcceptSyntaxTreeVisitor(_visitor);
            node.Update.AcceptSyntaxTreeVisitor(_visitor);
            VisitBody(node.Body);
        }

        public void VisitWhile(WhileNode node)
        {
            _visitor.VisitStatement(node);
            node.Condition.AcceptSyntaxTreeVisitor(_visitor);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            _visitor.VisitStatement(node);
            node.Expression.AcceptSyntaxTreeVisitor(_visitor);
        }

    }
}
