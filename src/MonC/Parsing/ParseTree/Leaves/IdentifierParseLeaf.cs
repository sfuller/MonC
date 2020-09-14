using MonC.SyntaxTree.Leaves;

namespace MonC.Parsing.ParseTreeLeaves
{
    public class IdentifierParseLeaf : IExpressionLeaf, IParseLeaf
    {
        public readonly string Name;

        public IdentifierParseLeaf(string name)
        {
            Name = name;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitIdentifier(this);
        }
    }
}
