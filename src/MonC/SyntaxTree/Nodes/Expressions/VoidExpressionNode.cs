namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VoidExpressionNode : ExpressionNode
    {
        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVoid(this);
        }
    }
}
