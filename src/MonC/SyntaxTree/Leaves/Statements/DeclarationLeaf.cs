
namespace MonC.SyntaxTree.Leaves.Statements
{
    public class DeclarationLeaf : IStatementLeaf
    {
        public TypeSpecifier Type;
        public string Name;
        public IExpressionLeaf Assignment;

        public DeclarationLeaf(TypeSpecifier type, string name, IExpressionLeaf assignment)
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
