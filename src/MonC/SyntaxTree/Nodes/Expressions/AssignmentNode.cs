namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AssignmentNode : BasicExpression
    {
        public IAddressableNode Lhs;
        public IExpressionNode Rhs;

        public AssignmentNode(IAddressableNode lhs, IExpressionNode rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }
}
