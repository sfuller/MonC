using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.ParseTree.Util
{
    public class ParseTreeVisitorExtension : IVisitor<IExpressionNode>
    {
        private readonly IParseTreeVisitor _visitor;

        public ParseTreeVisitorExtension(IParseTreeVisitor visitor)
        {
            _visitor = visitor;
        }

        public void Visit(IExpressionNode node)
        {
            if (node is IParseTreeNode parseTreeNode) {
                parseTreeNode.AcceptParseTreeVisitor(_visitor);
            }
        }
    }
}
