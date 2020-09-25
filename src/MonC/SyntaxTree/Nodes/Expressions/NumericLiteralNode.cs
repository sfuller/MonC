namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class NumericLiteralNode : ExpressionNode
    {
        public int Value;

        public NumericLiteralNode(int value)
        {
            Value = value;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitNumericLiteral(this);
        }
    }
}
