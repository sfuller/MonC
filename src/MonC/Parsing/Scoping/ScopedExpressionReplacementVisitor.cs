using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Parsing.Scoping
{
    public class ScopedExpressionReplacementVisitor : NoOpExpressionVisitor, IExpressionReplacementVisitor
    {
        private readonly IExpressionReplacementVisitor _innerVisitor;
        private readonly ScopeManager _scopeManager;

        public ScopedExpressionReplacementVisitor(IExpressionReplacementVisitor innerVisitor, ScopeManager scopeManager)
        {
            _innerVisitor = innerVisitor;
            _scopeManager = scopeManager;
        }

        public bool ShouldReplace => _innerVisitor.ShouldReplace;
        public IExpressionLeaf NewLeaf => _innerVisitor.NewLeaf;

        public void PrepareToVisit()
        {
            _innerVisitor.PrepareToVisit();
        }

        public override void VisitUnknown(IExpressionLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        protected override void VisitDefaultExpression(IExpressionLeaf leaf)
        {
            leaf.AcceptExpressionVisitor(_innerVisitor);
            if (_innerVisitor.ShouldReplace) {
                _scopeManager.ReplaceLeaf(leaf, _innerVisitor.NewLeaf);
            }
        }
    }
}
