using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class StatementChildrenVisitor : IStatementVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;
        private readonly ISyntaxTreeVisitor _childrenVisitor;

        public StatementChildrenVisitor(ISyntaxTreeVisitor visitor, ISyntaxTreeVisitor childrenVisitor)
        {
            _visitor = visitor;
            _childrenVisitor = childrenVisitor;
        }

        public void VisitBody(BodyNode node)
        {
            _visitor.VisitStatement(node);
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _visitor.VisitStatement(node);
            node.Type.AcceptSyntaxTreeVisitor(_childrenVisitor);
            node.Assignment.AcceptSyntaxTreeVisitor(_childrenVisitor);
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
            node.RHS.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }

        public void VisitIfElse(IfElseNode node)
        {
            _visitor.VisitStatement(node);
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            _visitor.VisitStatement(node);
            VisitDeclaration(node.Declaration);
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);
            node.Update.AcceptSyntaxTreeVisitor(_childrenVisitor);
            VisitBody(node.Body);
        }

        public void VisitWhile(WhileNode node)
        {
            _visitor.VisitStatement(node);
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            _visitor.VisitStatement(node);
            //node.Expression.AcceptSyntaxTreeVisitor(_visitor);
            node.Expression.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }

    }
}
