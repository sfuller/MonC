using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class FunctionCallNode : BasicExpression
    {
        public FunctionDefinitionNode LHS;
        private readonly IExpressionNode[] _arguments;

        public int ArgumentCount => _arguments.Length;

        public FunctionCallNode(FunctionDefinitionNode lhs, IEnumerable<IExpressionNode> arguments)
        {
            LHS = lhs;
            _arguments = arguments.ToArray();
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
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
