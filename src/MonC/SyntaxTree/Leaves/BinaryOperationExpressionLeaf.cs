namespace MonC.SyntaxTree
{
    public class BinaryOperationExpressionLeaf : IASTLeaf
    {
        public IASTLeaf LHS;
        public IASTLeaf RHS;
        public Token Op;

        public BinaryOperationExpressionLeaf(IASTLeaf lhs, IASTLeaf rhs, Token op)
        {
            LHS = lhs;
            RHS = rhs;
            Op = op;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitBinaryOperation(this);
        }
    }
}