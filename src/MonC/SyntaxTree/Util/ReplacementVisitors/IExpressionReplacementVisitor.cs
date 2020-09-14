using MonC.SyntaxTree.Leaves;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IExpressionReplacementVisitor : IExpressionVisitor
    {
        void PrepareToVisit();
        bool ShouldReplace { get; }
        IExpressionLeaf NewLeaf { get; }
    }
}
