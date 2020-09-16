using MonC.SyntaxTree.Leaves;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IStatementReplacementVisitor : IReplacementVisitor<IStatementLeaf>, IStatementVisitor
    {
    }
}
