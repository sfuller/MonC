using MonC.SyntaxTree.Nodes;
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
        public IExpressionNode NewNode => _innerVisitor.NewNode;

        public void PrepareToVisit()
        {
            _innerVisitor.PrepareToVisit();
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            VisitDefaultExpression(node);
        }

        protected override void VisitDefaultExpression(IExpressionNode node)
        {
            node.AcceptExpressionVisitor(_innerVisitor);
            if (_innerVisitor.ShouldReplace) {
                _scopeManager.ReplaceNode(node, _innerVisitor.NewNode);
            }
        }
    }
}
