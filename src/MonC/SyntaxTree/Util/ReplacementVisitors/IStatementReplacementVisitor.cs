using MonC.SyntaxTree.Leaves;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IStatementReplacementVisitor : IStatementVisitor
    {
        void PrepareToVisit();
        bool ShouldReplace { get; }
        IStatementLeaf NewLeaf { get; }
    }
}
