using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Parsing.Scoping
{
    public class ScopeManager : NoOpExpressionAndStatementVisitor, IScopeHandler, IParseTreeVisitor
    {
        private readonly Dictionary<ISyntaxTreeLeaf, Scope> _scopes = new Dictionary<ISyntaxTreeLeaf, Scope>();

        public void ProcessFunction(FunctionDefinitionLeaf function)
        {
            WalkScopeVisitor walkScopeVisitor = new WalkScopeVisitor(this, this, this, Scope.New(function));
            function.Body.AcceptStatements(walkScopeVisitor);
        }

        public Scope CurrentScope { get; set; }

        public void ReplaceLeaf(ISyntaxTreeLeaf oldLeaf, ISyntaxTreeLeaf newLeaf)
        {
            Scope scope = GetScope(oldLeaf);
            _scopes[newLeaf] = scope;
        }

        public Scope GetScope(ISyntaxTreeLeaf leaf)
        {
            if (!_scopes.TryGetValue(leaf, out Scope scope)) {
                return Scope.New();
            }
            return scope;
        }

        public override void VisitUnknown(IExpressionLeaf leaf)
        {
            if (leaf is IParseLeaf parseLeaf) {
                parseLeaf.AcceptParseTreeVisitor(this);
            }
        }

        protected override void VisitDefaultStatement(IStatementLeaf leaf)
        {
            ApplyScope(leaf);
        }

        protected override void VisitDefaultExpression(IExpressionLeaf leaf)
        {
            ApplyScope(leaf);
        }

        public void VisitAssignment(AssignmentParseLeaf leaf)
        {
            ApplyScope(leaf);
            ApplyScope(leaf.LHS);
            ApplyScope(leaf.RHS);
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            ApplyScope(leaf);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            ApplyScope(leaf);
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                IExpressionLeaf argument = leaf.GetArgument(i);
                argument.AcceptExpressionVisitor(this);
            }
        }

        private void ApplyScope(ISyntaxTreeLeaf leaf)
        {
            _scopes[leaf] = CurrentScope.Copy();
        }
    }
}
