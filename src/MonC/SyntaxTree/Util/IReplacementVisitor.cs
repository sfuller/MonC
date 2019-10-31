namespace MonC.SyntaxTree.Util
{
    public interface IReplacementVisitor : IASTLeafVisitor
    {
        bool ShouldReplace { get; }
        IASTLeaf? NewLeaf { get; }
    }
}