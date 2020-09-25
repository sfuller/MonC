using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.GenericDelegators;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics.Scoping
{
    public class ScopedExpressionReplacementVisitor : IVisitor<ISyntaxTreeNode>, IReplacementSource
    {
        private readonly IReplacementSource _innerSource;
        private readonly ScopeManager _scopeManager;

        private GenericSyntaxTreeDelegator _delegator;

        public ScopedExpressionReplacementVisitor(IReplacementSource innerSource, ScopeManager scopeManager)
        {
            _innerSource = innerSource;
            _scopeManager = scopeManager;

            _delegator = new GenericSyntaxTreeDelegator(this);
        }

        public ISyntaxTreeVisitor ReplacementVisitor => _delegator;
        public bool ShouldReplace => _innerSource.ShouldReplace;
        public ISyntaxTreeNode NewNode => _innerSource.NewNode;

        public void PrepareToVisit()
        {
            _innerSource.PrepareToVisit();
        }

        public void Visit(ISyntaxTreeNode node)
        {
            node.AcceptSyntaxTreeVisitor(_innerSource.ReplacementVisitor);
            if (_innerSource.ShouldReplace) {
                _scopeManager.ReplaceNode(node, _innerSource.NewNode);
            }
        }

    }
}
