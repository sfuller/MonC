namespace MonC.SyntaxTree
{
    public class NumericLiteralLeaf : IASTLeaf
    {
        public int Value;

        public NumericLiteralLeaf(int value)
        {
            Value = value;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}