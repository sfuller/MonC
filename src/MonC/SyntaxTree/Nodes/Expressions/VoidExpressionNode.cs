namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VoidExpressionNode : IExpressionNode
    {

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVoid(this);
        }
    }
}
