using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;

namespace MonC.Parsing.Semantics
{
    public interface IScopeHandler
    {
        Scope CurrentScope { set; }
    }

    public class WalkScopeVisitor : IStatementVisitor
    {
        private readonly IScopeHandler _scopeHandler;
        private readonly IStatementVisitor _statementVisitor;
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        private readonly ExpressionChildrenVisitor _expressionChildrenVisitor;

        public WalkScopeVisitor(
                IScopeHandler scopeHandler,
                IStatementVisitor statementVisitor,
                IExpressionVisitor expressionVisitor,
                Scope initialScope)
        {
            _scopeHandler = scopeHandler;
            _statementVisitor = statementVisitor;
            _scopes.Push(initialScope);

            // Scope can't change inside of expressions, so just use a children visitor for expressions.
            _expressionChildrenVisitor= new ExpressionChildrenVisitor(expressionVisitor);
        }

        public void VisitBody(BodyNode node)
        {
            VisitStatement(node);
            Scope baseScope = _scopes.Peek();
            _scopes.Push(baseScope.Copy());
            node.VisitStatements(this);
            _scopes.Pop();
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            VisitStatement(node);
            VisitExpression(node.Assignment);
            Scope scope = _scopes.Peek();
            scope.Variables.Add(node);
        }

        public void VisitBreak(BreakNode node)
        {
            VisitStatement(node);
        }

        public void VisitContinue(ContinueNode node)
        {
            VisitStatement(node);
        }

        public void VisitReturn(ReturnNode node)
        {
            VisitStatement(node);
            VisitExpression(node.RHS);
        }

        public void VisitIfElse(IfElseNode node)
        {
            VisitStatement(node);

            VisitExpression(node.Condition);
            VisitBody(node.IfBody);
            VisitBody(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            VisitStatement(node);

            Scope baseScope = _scopes.Peek();
            _scopes.Push(baseScope.Copy());
            VisitDeclaration(node.Declaration);
            VisitExpression(node.Condition);
            VisitExpression(node.Update);
            VisitBody(node.Body);
            _scopes.Pop();
        }

        public void VisitWhile(WhileNode node)
        {
            VisitStatement(node);

            VisitExpression(node.Condition);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            VisitStatement(node);
            VisitExpression(node.Expression);
        }

        private void VisitExpression(IExpressionNode node)
        {
            _scopeHandler.CurrentScope = _scopes.Peek();
            node.AcceptExpressionVisitor(_expressionChildrenVisitor);
        }

        private void VisitStatement(IStatementNode node)
        {
            Scope scope = _scopes.Peek();
            _scopeHandler.CurrentScope = scope;
            node.AcceptStatementVisitor(_statementVisitor);
        }
    }
}
