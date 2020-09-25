namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class StringLiteralNode : ExpressionNode
    {
        public string Value;

        public StringLiteralNode(string value)
        {
            Value = value;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
    }
}
