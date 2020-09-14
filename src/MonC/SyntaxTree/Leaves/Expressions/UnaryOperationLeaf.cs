namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class UnaryOperationLeaf : IExpressionLeaf
    {
        public readonly Token Operator;
        public IExpressionLeaf RHS;

        public UnaryOperationLeaf(Token @operator, IExpressionLeaf rhs)
        {
            Operator = @operator;
            RHS = rhs;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnaryOperation(this);
        }
    }
}
