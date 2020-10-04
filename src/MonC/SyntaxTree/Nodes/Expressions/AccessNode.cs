using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AccessNode : BasicExpression, IAssignableNode
    {
        public IExpressionNode Lhs;
        public DeclarationNode Rhs;

        public AccessNode(IExpressionNode lhs, DeclarationNode rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitAccess(this);
        }

        public void AcceptAssignableVisitor(IAssignableVisitor visitor)
        {
            visitor.VisitAccess(this);
        }
    }
}
