namespace MonC.SyntaxTree.Leaves.Statements
{
    public class WhileLeaf : IStatementLeaf
    {
        public IExpressionLeaf Condition;
        public BodyLeaf Body;

        public WhileLeaf(IExpressionLeaf condition, BodyLeaf body)
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
