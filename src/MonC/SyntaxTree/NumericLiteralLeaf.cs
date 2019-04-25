namespace MonC.SyntaxTree
{
    public class NumericLiteralLeaf : IASTLeaf
    {
        public string Value;

        public NumericLiteralLeaf(string value)
        {
            Value = value;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}