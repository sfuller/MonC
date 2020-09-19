using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VariableNode : ExpressionNode
    {
        public DeclarationNode Declaration;

        public VariableNode(DeclarationNode declaration)
        {
            Declaration = declaration;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVariable(this);
        }
    }
}
