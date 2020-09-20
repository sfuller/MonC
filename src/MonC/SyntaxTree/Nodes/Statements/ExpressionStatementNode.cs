namespace MonC.SyntaxTree.Nodes
{
    public class ExpressionStatementNode : StatementNode
    {
        public IExpressionNode Expression;

        public ExpressionStatementNode(IExpressionNode expression)
        {
            Expression = expression;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
    }
}
