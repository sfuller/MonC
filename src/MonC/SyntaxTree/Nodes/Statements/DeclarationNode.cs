
using MonC.SyntaxTree.Nodes.Specifiers;

namespace MonC.SyntaxTree.Nodes.Statements
{
    public class DeclarationNode : StatementNode
    {
        public ITypeSpecifierNode Type;
        public string Name;
        public IExpressionNode Assignment;

        public DeclarationNode(ITypeSpecifierNode type, string name, IExpressionNode assignment)
        {
            Type = type;
            Name = name;
            Assignment = assignment;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitDeclaration(this);
        }
    }
}
