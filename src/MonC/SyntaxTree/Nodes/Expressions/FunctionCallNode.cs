using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class FunctionCallNode : BasicExpression
    {
        public FunctionDefinitionNode LHS;
        public readonly List<IExpressionNode> Arguments = new List<IExpressionNode>();

        public FunctionCallNode(FunctionDefinitionNode lhs, IEnumerable<IExpressionNode> arguments)
        {
            LHS = lhs;
            Arguments.AddRange(arguments);
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

    }
}
