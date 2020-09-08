
namespace MonC.SyntaxTree
{
    public class DeclarationLeaf : IASTLeaf
    {
        public TypeSpecifierLeaf Type;
        public string Name;
        public IASTLeaf? Assignment;

        public DeclarationLeaf(TypeSpecifierLeaf type, string name, IASTLeaf? assignment)
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