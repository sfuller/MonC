using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class AccessParseNode : ExpressionNode, IParseTreeNode
    {
        public readonly IExpressionNode Lhs;
        public readonly DeclarationIdentifierParseNode Rhs;

        public AccessParseNode(IExpressionNode lhs, DeclarationIdentifierParseNode rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitAccess(this);
        }
    }
}
