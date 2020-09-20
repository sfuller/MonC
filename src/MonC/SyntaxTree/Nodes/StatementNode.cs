namespace MonC.SyntaxTree.Nodes
{
    public abstract class StatementNode : IStatementNode
    {
        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitStatement(this);
        }

        public abstract void AcceptStatementVisitor(IStatementVisitor visitor);
    }
}
