using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IStatementReplacementVisitor : IReplacementVisitor<IStatementNode>, IStatementVisitor
    {
    }
}
