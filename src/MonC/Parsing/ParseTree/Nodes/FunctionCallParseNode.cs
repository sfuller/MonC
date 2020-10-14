using System.Collections.Generic;
using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class FunctionCallParseNode : IExpressionNode, IParseTreeNode
    {
        public IExpressionNode LHS;
        public readonly List<IExpressionNode> Arguments = new List<IExpressionNode>();

        public FunctionCallParseNode(IExpressionNode lhs, IEnumerable<IExpressionNode> arguments)
        {
            LHS = lhs;
            Arguments.AddRange(arguments);
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitExpression(this);
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

    }
}
