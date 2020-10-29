using MonC.Semantics.Scoping;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics
{
    public class FunctionReplacementHandler : IReplacementListener
    {
        private readonly ScopeManager _scopeManager;

        public FunctionReplacementHandler(ScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
        }

        public void NodeReplaced(ISyntaxTreeNode oldNode, ISyntaxTreeNode newNode)
        {
            _scopeManager.ReplaceNode(oldNode, newNode);

            // TODO: Replace symbols!
        }
    }
}
