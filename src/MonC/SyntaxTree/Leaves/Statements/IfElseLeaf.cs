namespace MonC.SyntaxTree.Leaves.Statements
{
    public class IfElseLeaf : IStatementLeaf
    {
        public IExpressionLeaf Condition;
        public Body IfBody;
        public Body ElseBody;

        public IfElseLeaf(IExpressionLeaf condition, Body ifBody, Body elseBody)
        {
            Condition = condition;
            IfBody = ifBody;
            ElseBody = elseBody;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitIfElse(this);
        }

    }
}
