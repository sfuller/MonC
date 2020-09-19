namespace MonC.SyntaxTree.Nodes.Statements
{
    public class ContinueNode : StatementNode
    {
        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitContinue(this);
        }
    }
}
