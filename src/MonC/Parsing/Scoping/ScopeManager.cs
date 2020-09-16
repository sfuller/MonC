using System.Collections.Generic;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Parsing.Scoping
{
    public class ScopeManager : NoOpExpressionAndStatementVisitor, IScopeHandler, IParseTreeVisitor
    {
        private readonly Dictionary<ISyntaxTreeNode, Scope> _scopes = new Dictionary<ISyntaxTreeNode, Scope>();

        public void ProcessFunction(FunctionDefinitionNode function)
        {
            WalkScopeVisitor walkScopeVisitor = new WalkScopeVisitor(this, this, this, Scope.New(function));
            walkScopeVisitor.VisitBody(function.Body);
        }

        public Scope CurrentScope { get; set; }

        public void ReplaceNode(ISyntaxTreeNode oldNode, ISyntaxTreeNode newNode)
        {
            Scope scope = GetScope(oldNode);
            _scopes[newNode] = scope;
        }

        public Scope GetScope(ISyntaxTreeNode node)
        {
            if (!_scopes.TryGetValue(node, out Scope scope)) {
                return Scope.New();
            }
            return scope;
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            }
        }

        protected override void VisitDefaultStatement(IStatementNode node)
        {
            ApplyScope(node);
        }

        protected override void VisitDefaultExpression(IExpressionNode node)
        {
            ApplyScope(node);
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            ApplyScope(node);
            ApplyScope(node.LHS);
            ApplyScope(node.RHS);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            ApplyScope(node);
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            ApplyScope(node);
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                IExpressionNode argument = node.GetArgument(i);
                argument.AcceptExpressionVisitor(this);
            }
        }

        private void ApplyScope(ISyntaxTreeNode node)
        {
            _scopes[node] = CurrentScope.Copy();
        }
    }
}
