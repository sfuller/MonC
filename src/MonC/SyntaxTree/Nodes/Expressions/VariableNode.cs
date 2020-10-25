using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VariableNode : BasicExpression, IAssignableNode
    {
        public DeclarationNode Declaration;

        public VariableNode(DeclarationNode declaration)
        {
            Declaration = declaration;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitVariable(this);
        }

        public void AcceptAssignableVisitor(IAssignableVisitor visitor)
        {
            visitor.VisitVariable(this);
        }
    }
}
