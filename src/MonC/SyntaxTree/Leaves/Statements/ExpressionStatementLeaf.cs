namespace MonC.SyntaxTree.Leaves
{
    public class ExpressionStatementLeaf : IStatementLeaf
    {
        public IExpressionLeaf Expression;

        public ExpressionStatementLeaf(IExpressionLeaf expression)
        {
            Expression = expression;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
    }
}
