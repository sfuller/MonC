
namespace MonC.SyntaxTree
{
    public class DeclarationLeaf : IASTLeaf
    {
        public string Type;
        public string Name;
        public Optional<IASTLeaf> Assignment;

        public DeclarationLeaf(string type, string name, Optional<IASTLeaf> assignment)
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