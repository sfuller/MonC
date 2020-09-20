namespace MonC.SyntaxTree.Nodes.Statements
{
    public class WhileNode : StatementNode
    {
        public IExpressionNode Condition;
        public BodyNode Body;

        public WhileNode(IExpressionNode condition, BodyNode body)
        {
            Condition = condition;
            Body = body;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitWhile(this);
        }
    }
}
