namespace MonC.SyntaxTree.Nodes.Statements
{
    public class ContinueNode : IStatementNode
    {
        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitContinue(this);
        }
    }
}
