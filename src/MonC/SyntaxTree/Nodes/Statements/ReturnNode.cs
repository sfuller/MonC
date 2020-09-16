namespace MonC.SyntaxTree.Nodes.Statements
{
    public class ReturnNode : IStatementNode
    {
        public IExpressionNode RHS;

        public ReturnNode(IExpressionNode rhs)
        {
            RHS = rhs;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }
}
