using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class VariableNode : BasicExpression
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
    }
}
