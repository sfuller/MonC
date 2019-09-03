namespace MonC.Parsing.ParseTreeLeaves
{
    public class IdentifierParseLeaf : IASTLeaf
    {
        public readonly Token Token;
        public readonly string Name;

        public IdentifierParseLeaf(string name, Token token)
        {
            Name = name;
            Token = token;
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