namespace MonC.SyntaxTree.Nodes.Statements
{
    public class BreakNode : StatementNode
    {
        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitBreak(this);
        }
    }
}
