namespace MonC.SyntaxTree
{
    public class StringLiteralLeaf : IASTLeaf
    {
        public string Value;

        public StringLiteralLeaf(string value)
        {
            Value = value;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}