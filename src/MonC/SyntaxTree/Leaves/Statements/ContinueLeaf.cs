namespace MonC.SyntaxTree.Leaves.Statements
{
    public class ContinueLeaf : IStatementLeaf
    {
        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitContinue(this);
        }
    }
}
