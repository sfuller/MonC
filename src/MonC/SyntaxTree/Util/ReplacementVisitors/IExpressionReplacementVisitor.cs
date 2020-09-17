using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IExpressionReplacementVisitor : IReplacementVisitor<IExpressionNode>, IExpressionVisitor
    {
    }
}
