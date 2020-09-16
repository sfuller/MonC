namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IReplacementVisitor<out T> where T : ISyntaxTreeLeaf
    {
        void PrepareToVisit();
        bool ShouldReplace { get; }
        T NewLeaf { get; }
    }
}
