namespace MonC.SyntaxTree.Leaves.Statements
{
    public class WhileLeaf : IStatementLeaf
    {
        public IExpressionLeaf Condition;
        public Body Body;

        public WhileLeaf(IExpressionLeaf condition, Body body)
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
