using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AccessNode : BasicExpression, IAddressableNode
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

        public void AcceptAddressableVisitor(IAddressableVisitor visitor)
        {
            visitor.VisitAccess(this);
        }

        public bool IsAddressable()
        {
            if (Lhs is IAddressableNode assignableLhs) {
                return assignableLhs.IsAddressable();
            }
            return false;
        }
    }
}
