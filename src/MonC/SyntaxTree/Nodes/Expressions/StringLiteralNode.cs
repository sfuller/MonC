namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class StringLiteralNode : IExpressionNode
    {
        public string Value;

        public StringLiteralNode(string value)
        {
            Value = value;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}
