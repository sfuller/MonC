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
            IParseTreeLeafVisitor specializedVisitor = visitor as IParseTreeLeafVisitor;
            if (specializedVisitor != null) {
                specializedVisitor.VisitIdentifier(this);
            }
        }
    }
}