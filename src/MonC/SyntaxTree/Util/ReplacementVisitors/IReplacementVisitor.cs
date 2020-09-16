namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IReplacementVisitor<out T> where T : ISyntaxTreeNode
    {
        void PrepareToVisit();
        bool ShouldReplace { get; }
        T NewNode { get; }
    }
}
