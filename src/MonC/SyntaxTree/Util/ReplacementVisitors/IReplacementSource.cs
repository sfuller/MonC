using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IReplacementSource
    {
        void PrepareToVisit();
        ISyntaxTreeVisitor ReplacementVisitor { get; }
        bool ShouldReplace { get; }
        ISyntaxTreeNode NewNode { get; }
    }
}
