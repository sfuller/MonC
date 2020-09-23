using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics.Scoping
{
    public class ScopedExpressionReplacementVisitor : ISyntaxTreeVisitor, IReplacementSource
    {
        private readonly IReplacementSource _innerSource;
        private readonly ScopeManager _scopeManager;

        public ScopedExpressionReplacementVisitor(IReplacementSource innerSource, ScopeManager scopeManager)
        {
            _innerSource = innerSource;
            _scopeManager = scopeManager;
        }

        public ISyntaxTreeVisitor ReplacementVisitor => this;
        public bool ShouldReplace => _innerSource.ShouldReplace;
        public ISyntaxTreeNode NewNode => _innerSource.NewNode;

        public void PrepareToVisit()
        {
            _innerSource.PrepareToVisit();
        }

        private void VisitDefault(ISyntaxTreeNode node)
        {
            node.AcceptSyntaxTreeVisitor(_innerSource.ReplacementVisitor);
            if (_innerSource.ShouldReplace) {
                _scopeManager.ReplaceNode(node, _innerSource.NewNode);
            }
        }

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
            VisitDefault(node);
        }

        public void VisitStatement(IStatementNode node)
        {
            VisitDefault(node);
        }

        public void VisitExpression(IExpressionNode node)
        {
            VisitDefault(node);
        }

        public void VisitSpecifier(ISpecifierNode node)
        {
            VisitDefault(node);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationNode node)
        {
            VisitDefault(node);
        }
    }
}
