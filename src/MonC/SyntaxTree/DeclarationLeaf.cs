
namespace MonC.SyntaxTree
{
    public class DeclarationLeaf : IASTLeaf
    {
        private readonly string Type;
        private readonly string Name;
        private readonly IASTLeaf Assignment;

        public DeclarationLeaf(string type, string name, IASTLeaf assignment)
        {
            Type = type;
            Name = name;
            Assignment = assignment;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitDeclaration(this);
        }
    }
}