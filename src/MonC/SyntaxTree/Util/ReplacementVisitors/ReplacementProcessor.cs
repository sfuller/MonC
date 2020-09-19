using System;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public readonly struct ReplacementProcessor
    {
        private readonly IReplacementSource _replacementSource;

        public ReplacementProcessor(IReplacementSource replacementSource)
        {
            _replacementSource = replacementSource;
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

            return replacement;
        }
    }
}
