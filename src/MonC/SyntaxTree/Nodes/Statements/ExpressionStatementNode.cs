namespace MonC.SyntaxTree.Nodes
{
    public class ExpressionStatementNode : IStatementNode
    {
        public IExpressionNode Expression;

        public ExpressionStatementNode(IExpressionNode expression)
        {
            Expression = expression;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
    }
}
