using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class SyntaxTreeDelegator : ISyntaxTreeVisitor
    {
        public ITopLevelStatementVisitor? TopLevelVisitor;
        public IStatementVisitor? StatementVisitor;
        public IExpressionVisitor? ExpressionVisitor;
        public ISpecifierVisitor? SpecifierVisitor;

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
            if (TopLevelVisitor != null) {
                node.AcceptTopLevelVisitor(TopLevelVisitor);
            }
        }

        public void VisitStatement(IStatementNode node)
        {
            if (StatementVisitor != null) {
                node.AcceptStatementVisitor(StatementVisitor);
            }
        }

        public void VisitExpression(IExpressionNode node)
        {
            if (ExpressionVisitor != null) {
                node.AcceptExpressionVisitor(ExpressionVisitor);
            }
        }

        public void VisitSpecifier(ISpecifierNode node)
        {
            if (SpecifierVisitor != null) {
                node.AcceptSpecifierVisitor(SpecifierVisitor);
            }
        }
    }
}
