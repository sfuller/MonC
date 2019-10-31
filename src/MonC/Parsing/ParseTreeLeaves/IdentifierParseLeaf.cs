namespace MonC.Parsing.ParseTreeLeaves
{
    public class IdentifierParseLeaf : IASTLeaf
    {
        public readonly string Name;

        public IdentifierParseLeaf(string name)
        {
            Name = name;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            if (visitor is IParseTreeLeafVisitor specializedVisitor) {
                specializedVisitor.VisitIdentifier(this);
            }
        }
    }
}