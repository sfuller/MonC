namespace MonC.SyntaxTree.Nodes.Statements
{
    public class WhileNode : IStatementNode
    {
        public IExpressionNode Condition;
        public BodyNode Body;

        public WhileNode(IExpressionNode condition, BodyNode body)
        {
            Condition = condition;
            Body = body;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitWhile(this);
        }
    }
}
