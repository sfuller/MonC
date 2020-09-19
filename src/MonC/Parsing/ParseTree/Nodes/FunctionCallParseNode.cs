using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class FunctionCallParseNode : IExpressionNode, IParseTreeNode
    {
        public readonly IExpressionNode LHS;
        private readonly IExpressionNode[] _arguments;

        public int ArgumentCount => _arguments.Length;

        public IExpressionNode[] GetArguments()
        {
            // TODO: This sucks. Since nodes are mutable, we should just make this a list and allow access to it.
            return new List<IExpressionNode>(_arguments).ToArray();
        }

        public FunctionCallParseNode(IExpressionNode lhs, IEnumerable<IExpressionNode> arguments)
        {
            LHS = lhs;
            _arguments = arguments.ToArray();
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

        public IExpressionNode GetArgument(int index)
        {
            return _arguments[index];
        }

        public void SetArgument(int index, IExpressionNode argument)
        {
            _arguments[index] = argument;
        }
    }
}
