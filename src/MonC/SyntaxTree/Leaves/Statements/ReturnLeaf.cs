namespace MonC.SyntaxTree.Leaves.Statements
{
    public class ReturnLeaf : IStatementLeaf
    {
        public IExpressionLeaf RHS;

        public ReturnLeaf(IExpressionLeaf rhs)
        {
            RHS = rhs;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }
}
