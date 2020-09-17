namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class NumericLiteralNode : IExpressionNode
    {
        public int Value;

        public NumericLiteralNode(int value)
        {
            Value = value;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}
