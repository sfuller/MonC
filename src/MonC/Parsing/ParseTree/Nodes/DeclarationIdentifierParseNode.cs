using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class DeclarationIdentifierParseNode : IParseTreeNode
    {
        public readonly string Name;

        public DeclarationIdentifierParseNode(string name)
        {
            Name = name;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitDeclarationIdentifier(this);
        }
    }
}
