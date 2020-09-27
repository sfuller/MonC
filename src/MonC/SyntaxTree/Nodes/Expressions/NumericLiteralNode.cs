namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class NumericLiteralNode : BasicExpression
    {
        public int Value;

        public NumericLiteralNode(int value)
        {
            Value = value;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}
