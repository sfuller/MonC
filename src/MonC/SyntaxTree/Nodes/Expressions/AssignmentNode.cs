using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class AssignmentNode : ExpressionNode
    {
        public DeclarationNode Declaration;
        public IExpressionNode RHS;

        public AssignmentNode(DeclarationNode declaration, IExpressionNode rhs)
        {
            Declaration = declaration;
            RHS = rhs;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }

    }
}
