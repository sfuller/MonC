namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AssignmentNode : BasicExpression
    {
        public IAssignableNode Lhs;
        public IExpressionNode Rhs;

        public AssignmentNode(IAssignableNode lhs, IExpressionNode rhs)
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
