namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class StringLiteralNode : BasicExpression
    {
        public string Value;

        public StringLiteralNode(string value)
        {
            Value = value;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}
