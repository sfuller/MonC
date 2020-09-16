using MonC.SyntaxTree.Leaves;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IExpressionReplacementVisitor : IReplacementVisitor<IExpressionLeaf>, IExpressionVisitor
    {
    }
}
