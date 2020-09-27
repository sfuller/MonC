using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessReplacementsVisitorChain
    {
        private readonly ExpressionChildrenVisitor _expressionChildrenVisitor;
        private readonly StatementChildrenVisitor _statementChildrenVisitor;
        private readonly TopLevelStatementChildrenVisitor _topLevelStatementChildrenVisitor;

        public ProcessReplacementsVisitorChain(IReplacementSource source)
        {
            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor =
                new ProcessExpressionReplacementsVisitor(source);
            ProcessStatementReplacementsVisitor statementReplacementsVisitor =
                new ProcessStatementReplacementsVisitor(source);
            ProcessTopLevelStatementReplacementsVisitor topLevelStatementReplacementsVisitor =
                new ProcessTopLevelStatementReplacementsVisitor(source);

            // Configure the expression children visitor to use the expression replacements visitor for expressions.
            SyntaxTreeDelegator expressionChildrenDelegator = new SyntaxTreeDelegator();
            expressionChildrenDelegator.ExpressionVisitor = expressionReplacementsVisitor;
            expressionChildrenDelegator.StatementVisitor = statementReplacementsVisitor;
            _expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionChildrenDelegator);

            // Configure the statement children visitor to use the expression children visitor when encountering expressions.
            SyntaxTreeDelegator statementChildrenDelegator = new SyntaxTreeDelegator();
            statementChildrenDelegator.ExpressionVisitor = _expressionChildrenVisitor;
            statementChildrenDelegator.StatementVisitor = statementReplacementsVisitor;
            _statementChildrenVisitor = new StatementChildrenVisitor(statementChildrenDelegator);

            // Configure the top-level statement children visitor to use the other visitors.
            SyntaxTreeDelegator topLevelStatementChildrenDelegator = new SyntaxTreeDelegator();
            topLevelStatementChildrenDelegator.ExpressionVisitor = _expressionChildrenVisitor;
            topLevelStatementChildrenDelegator.StatementVisitor = _statementChildrenVisitor;
            topLevelStatementChildrenDelegator.TopLevelVisitor = topLevelStatementReplacementsVisitor;
            _topLevelStatementChildrenVisitor =
                new TopLevelStatementChildrenVisitor(topLevelStatementChildrenDelegator);
        }

        public void ProcessReplacements(ExpressionNode node)
        {
            node.AcceptExpressionVisitor(_expressionChildrenVisitor);
        }

        public void ProcessReplacements(StatementNode node)
        {
            node.AcceptStatementVisitor(_statementChildrenVisitor);
        }

        public void ProcessReplacements(ITopLevelStatementNode node)
        {
            node.AcceptTopLevelVisitor(_topLevelStatementChildrenVisitor);
        }
    }
}
