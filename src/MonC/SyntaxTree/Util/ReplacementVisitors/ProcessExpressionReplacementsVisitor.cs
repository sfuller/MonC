using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessExpressionReplacementsVisitor : IExpressionVisitor
    {
        public readonly IExpressionReplacementVisitor _replacer;

        public ProcessExpressionReplacementsVisitor(IExpressionReplacementVisitor replacer)
        {
            _replacer = replacer;
        }

        public void VisitVoid(VoidExpressionNode node)
        {
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
        }

        public void VisitEnumValue(EnumValueNode node)
        {
        }

        public void VisitVariable(VariableNode node)
        {
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            node.RHS = ProcessReplacement(node.RHS);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            // TODO: Should this be optional to allow more flexibility with a IBinaryOperationVisitor?
            node.LHS = ProcessReplacement(node.LHS);
            node.RHS = ProcessReplacement(node.RHS);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                node.SetArgument(i, ProcessReplacement(node.GetArgument(i)));
            }
        }

        public void VisitAssignment(AssignmentNode node)
        {
            node.RHS = ProcessReplacement(node.RHS);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            // TODO: Should we do anything here?
        }

        private IExpressionNode ProcessReplacement(IExpressionNode node)
        {
            _replacer.PrepareToVisit();
            node.AcceptExpressionVisitor(_replacer);

            if (!_replacer.ShouldReplace) {
                return node;
            }

            return _replacer.NewNode;
        }
    }
}
