using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree.Leaves;

namespace MonC.Parsing.ParseTreeLeaves
{
    public class FunctionCallParseLeaf : IExpressionLeaf, IParseLeaf
    {
        public readonly IExpressionLeaf LHS;
        private readonly IExpressionLeaf[] _arguments;

        public int ArgumentCount => _arguments.Length;

        public IExpressionLeaf[] GetArguments()
        {
            // TODO: This sucks. Since leaves are mutable, we should just make this a list and allow access to it.
            return new List<IExpressionLeaf>(_arguments).ToArray();
        }

        public FunctionCallParseLeaf(IExpressionLeaf lhs, IEnumerable<IExpressionLeaf> arguments)
        {
            LHS = lhs;
            _arguments = arguments.ToArray();
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

        public IExpressionLeaf GetArgument(int index)
        {
            return _arguments[index];
        }

        public void SetArgument(int index, IExpressionLeaf argument)
        {
            _arguments[index] = argument;
        }
    }
}
