using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AssignmentNode : IExpressionNode
    {
        public DeclarationNode Declaration;
        public IExpressionNode RHS;

        public AssignmentNode(DeclarationNode declaration, IExpressionNode rhs)
        {
            Declaration = declaration;
            RHS = rhs;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }

    }
}
