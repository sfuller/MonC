namespace MonC.SyntaxTree.Nodes.Statements
{
    public class BreakNode : IStatementNode
    {
        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitBreak(this);
        }
    }
}
