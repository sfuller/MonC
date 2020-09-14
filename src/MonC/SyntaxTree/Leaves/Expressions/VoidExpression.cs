namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class VoidExpression : IExpressionLeaf
    {

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVoid(this);
        }
    }
}
