
namespace MonC.SyntaxTree
{
    public class DeclarationLeaf : IASTLeaf
    {
        public readonly Token Token; 
        public string Type;
        public string Name;
        public Optional<IASTLeaf> Assignment;

        public DeclarationLeaf(string type, string name, Optional<IASTLeaf> assignment, Token token)
        {
            Type = type;
            Name = name;
            Assignment = assignment;
            Token = token;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitDeclaration(this);
        }
    }
}