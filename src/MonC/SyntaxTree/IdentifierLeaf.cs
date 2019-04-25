namespace MonC.SyntaxTree
{
    public class IdentifierLeaf : IASTLeaf
    {
        public string Name;

        public IdentifierLeaf(string name)
        {
            Name = name;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitIdentifier(this);
        }
    }
}