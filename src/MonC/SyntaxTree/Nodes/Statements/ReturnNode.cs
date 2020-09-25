namespace MonC.SyntaxTree.Nodes.Statements
{
    public class ReturnNode : StatementNode
    {
        public IExpressionNode RHS;

        public ReturnNode(IExpressionNode rhs)
        {
            RHS = rhs;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }
}
