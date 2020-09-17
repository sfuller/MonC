using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class IdentifierParseNode : IExpressionNode, IParseTreeNode
    {
        public readonly string Name;

        public IdentifierParseNode(string name)
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
