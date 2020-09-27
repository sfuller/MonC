using System.Collections.Generic;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.GenericDelegators;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Semantics.Scoping
{
    public class ScopeManager : NoOpStatementVisitor, IScopeHandler, IParseTreeVisitor, IVisitor<IExpressionNode>
    {
        private readonly Dictionary<ISyntaxTreeNode, Scope> _scopes = new Dictionary<ISyntaxTreeNode, Scope>();

        private readonly SyntaxTreeDelegator _delegator = new SyntaxTreeDelegator();

        public ScopeManager()
        {
            _delegator.StatementVisitor = this;
            _delegator.ExpressionVisitor = new GenericExpressionDelegator(this);
        }

        public void ProcessFunction(FunctionDefinitionNode function)
        {
            WalkScopeVisitor walkScopeVisitor = new WalkScopeVisitor(this, _delegator, Scope.New(function));
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

        public void Visit(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            } else {
                ApplyScope(node);
            }
        }

        protected override void VisitDefaultStatement(IStatementNode node)
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
                argument.AcceptSyntaxTreeVisitor(_delegator);
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            ApplyScope(node);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            ApplyScope(node);
        }

        private void ApplyScope(ISyntaxTreeNode node)
        {
            _scopes[node] = CurrentScope.Copy();
        }
    }
}
