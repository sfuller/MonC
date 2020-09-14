namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class NumericLiteralLeaf : IExpressionLeaf
    {
        public int Value;

        public NumericLiteralLeaf(int value)
        {
            Value = value;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}
