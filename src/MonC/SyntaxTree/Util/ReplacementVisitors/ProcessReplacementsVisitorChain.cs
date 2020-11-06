using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessReplacementsVisitorChain
    {
        public readonly ExpressionChildrenVisitor ExpressionChildrenVisitor;
        public readonly StatementChildrenVisitor StatementChildrenVisitor;
        public readonly TopLevelStatementChildrenVisitor TopLevelStatementChildrenVisitor;

        public readonly ProcessExpressionReplacementsVisitor ExpressionReplacementsVisitor;
        public readonly ProcessStatementReplacementsVisitor StatementReplacementsVisitor;
        public readonly ProcessTopLevelStatementReplacementsVisitor TopLevelStatementReplacementsVisitor;

        public readonly SyntaxTreeDelegator ChildrenVisitor;
        public readonly SyntaxTreeDelegator ReplacementVisitor;

        public ProcessReplacementsVisitorChain(IReplacementSource source, IReplacementListener listener, bool isPostOrder = false)
        {
            ExpressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(source, listener);
            StatementReplacementsVisitor = new ProcessStatementReplacementsVisitor(source, listener);
            TopLevelStatementReplacementsVisitor = new ProcessTopLevelStatementReplacementsVisitor(source, listener);

            ReplacementVisitor = new SyntaxTreeDelegator();
            ReplacementVisitor.ExpressionVisitor = ExpressionReplacementsVisitor;
            ReplacementVisitor.StatementVisitor = StatementReplacementsVisitor;
            ReplacementVisitor.TopLevelVisitor = TopLevelStatementReplacementsVisitor;

            ChildrenVisitor = new SyntaxTreeDelegator();
            ExpressionChildrenVisitor = new ExpressionChildrenVisitor(
                preOrderVisitor: isPostOrder ? null : ReplacementVisitor,
                postOrderVisitor: isPostOrder ? ReplacementVisitor : null,
                childrenVisitor: ChildrenVisitor);
            StatementChildrenVisitor = new StatementChildrenVisitor(ReplacementVisitor, ChildrenVisitor);
            TopLevelStatementChildrenVisitor = new TopLevelStatementChildrenVisitor(ReplacementVisitor, ChildrenVisitor);

            ChildrenVisitor.ExpressionVisitor = ExpressionChildrenVisitor;
            ChildrenVisitor.StatementVisitor = StatementChildrenVisitor;
            ChildrenVisitor.TopLevelVisitor = TopLevelStatementChildrenVisitor;
        }

        public void ProcessReplacements(ExpressionNode node)
        {
            node.AcceptExpressionVisitor(ExpressionChildrenVisitor);
        }

        public void ProcessReplacements(StatementNode node)
        {
            node.AcceptStatementVisitor(StatementChildrenVisitor);
        }

        public void ProcessReplacements(ITopLevelStatementNode node)
        {
            node.AcceptTopLevelVisitor(TopLevelStatementChildrenVisitor);
        }
    }
}
