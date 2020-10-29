using System;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public readonly struct ReplacementProcessor
    {
        private readonly IReplacementSource _replacementSource;
        private readonly IReplacementListener _listener;

        public ReplacementProcessor(IReplacementSource replacementSource, IReplacementListener listener)
        {
            _replacementSource = replacementSource;
            _listener = listener;
        }

        public T ProcessReplacement<T>(T node) where T : ISyntaxTreeNode
        {
            _replacementSource.PrepareToVisit();
            node.AcceptSyntaxTreeVisitor(_replacementSource.ReplacementVisitor);

            if (!_replacementSource.ShouldReplace) {
                return node;
            }

            // TODO: Add static analysis for this.
            if (!(_replacementSource.NewNode is T replacement)) {
                throw new InvalidOperationException("Cannot replace, type mismatch");
            }

            _listener.NodeReplaced(node, replacement);

            return replacement;
        }
    }
}
