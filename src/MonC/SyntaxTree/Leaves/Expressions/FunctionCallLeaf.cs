using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class FunctionCallLeaf : IExpressionLeaf
    {
        public FunctionDefinitionLeaf LHS;
        private readonly IExpressionLeaf[] _arguments;

        public int ArgumentCount => _arguments.Length;

        public FunctionCallLeaf(FunctionDefinitionLeaf lhs, IEnumerable<IExpressionLeaf> arguments)
        {
            LHS = lhs;
            _arguments = arguments.ToArray();
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
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
