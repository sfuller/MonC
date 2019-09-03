namespace MonC.SyntaxTree
{
    public class UnaryOperationLeaf : IASTLeaf
    {
        public readonly Token Operator;
        public IASTLeaf RHS;

        public UnaryOperationLeaf(Token @operator, IASTLeaf rhs)
        {
            Operator = @operator;
            RHS = rhs;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitUnaryOperation(this);
        }
    }
}