namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VoidExpressionNode : BasicExpression
    {
        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitVoid(this);
        }
    }
}
