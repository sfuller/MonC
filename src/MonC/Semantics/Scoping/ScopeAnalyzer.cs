using System;
using MonC.Parsing.ParseTree.Util;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.GenericDelegators;

namespace MonC.Semantics.Scoping
{
    public class ScopeAnalyzer : IStatementVisitor, IVisitor<IExpressionNode>
    {
        private readonly ScopeManager _scopeManager;
        private Scope _currentScope = new Scope();

        private readonly ExpressionChildrenVisitor _expressionChildrenVisitor;

        /// <summary>
        /// Represents the scope of "Outside the current function".
        /// </summary>
        public Scope OuterScope { get; }

        public ScopeAnalyzer(ScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
            SyntaxTreeDelegator visitor = new SyntaxTreeDelegator();
            visitor.StatementVisitor = this;
            visitor.ExpressionVisitor = new GenericExpressionDelegator(this);

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            _expressionChildrenVisitor = new ExpressionChildrenVisitor(visitor, null, childrenVisitor);
            _expressionChildrenVisitor.ExtensionChildrenVisitor = new ParseTreeVisitorExtension(
                    new ParseTreeChildrenVisitor(visitor, null, childrenVisitor));
            childrenVisitor.ExpressionVisitor = _expressionChildrenVisitor;

            OuterScope = _currentScope;
        }

        public void Analyze(FunctionDefinitionNode function)
        {
            OuterScope.Variables.AddRange(function.Parameters);
            PushScope();
            function.Body.VisitStatements(this);
            PopScope();
        }

        private void PopScope()
        {
            Scope? parent = _currentScope.Parent;
            if (parent == null) {
                throw new InvalidOperationException();
            }
            _currentScope = parent;
        }

        private void PushScope()
        {
            _currentScope = new Scope(_currentScope, _currentScope.Variables.Count);
        }

        private void RecurseExpression(IExpressionNode expression)
        {
            expression.AcceptExpressionVisitor(_expressionChildrenVisitor);
        }

        public void VisitBody(BodyNode node)
        {
            PushScope();
            node.VisitStatements(this);
            PopScope();
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            _currentScope.Variables.Add(node);
            RecurseExpression(node.Assignment);
        }

        public void VisitBreak(BreakNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
        }

        public void VisitContinue(ContinueNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
        }

        public void VisitReturn(ReturnNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            RecurseExpression(node.RHS);
        }

        public void VisitIfElse(IfElseNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            RecurseExpression(node.Condition);
            node.IfBody.AcceptStatementVisitor(this);
            node.ElseBody.AcceptStatementVisitor(this);
        }

        public void VisitFor(ForNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            PushScope();
            node.Declaration.AcceptStatementVisitor(this);
            RecurseExpression(node.Condition);
            RecurseExpression(node.Update);
            VisitBody(node.Body);
            PopScope();
        }

        public void VisitWhile(WhileNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            RecurseExpression(node.Condition);
            VisitBody(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
            RecurseExpression(node.Expression);
        }

        public void Visit(IExpressionNode node)
        {
            _scopeManager.SetScope(node, _currentScope);
        }
    }
}
