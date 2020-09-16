namespace MonC.SyntaxTree.Leaves.Statements
{
    public class IfElseLeaf : IStatementLeaf
    {
        public IExpressionLeaf Condition;
        public BodyLeaf IfBody;
        public BodyLeaf ElseBody;

        public IfElseLeaf(IExpressionLeaf condition, BodyLeaf ifBody, BodyLeaf elseBody)
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
