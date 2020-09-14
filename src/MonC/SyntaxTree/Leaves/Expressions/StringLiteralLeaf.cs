namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class StringLiteralLeaf : IExpressionLeaf
    {
        public string Value;

        public StringLiteralLeaf(string value)
        {
            Value = value;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}
