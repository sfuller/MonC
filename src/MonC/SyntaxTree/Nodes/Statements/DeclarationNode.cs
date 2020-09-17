
namespace MonC.SyntaxTree.Nodes.Statements
{
    public class DeclarationNode : IStatementNode
    {
        public TypeSpecifier Type;
        public string Name;
        public IExpressionNode Assignment;

        public DeclarationNode(TypeSpecifier type, string name, IExpressionNode assignment)
        {
            Type = type;
            Name = name;
            Assignment = assignment;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitDeclaration(this);
        }
    }
}
