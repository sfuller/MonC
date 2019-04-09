namespace MonC.SyntaxTree
{
    public class BinaryOperationExpressionLeaf : IASTLeaf
    {
        public readonly IASTLeaf LHS;
        public readonly IASTLeaf RHS;
        public readonly Token Op;

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