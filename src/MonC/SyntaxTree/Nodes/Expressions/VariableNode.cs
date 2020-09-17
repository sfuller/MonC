using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VariableNode : IExpressionNode
    {
        public DeclarationNode Declaration;

        public VariableNode(DeclarationNode declaration)
        {
            Declaration = declaration;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVariable(this);
        }
    }
}
