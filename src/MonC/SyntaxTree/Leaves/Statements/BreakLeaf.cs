namespace MonC.SyntaxTree.Leaves.Statements
{
    public class BreakLeaf : IStatementLeaf
    {
        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitBreak(this);
        }
    }
}
