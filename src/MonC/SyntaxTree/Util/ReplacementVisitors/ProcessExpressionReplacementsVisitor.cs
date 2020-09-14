using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessExpressionReplacementsVisitor : IExpressionVisitor
    {
        public readonly IExpressionReplacementVisitor _replacer;

        public ProcessExpressionReplacementsVisitor(IExpressionReplacementVisitor replacer)
        {
            _replacer = replacer;
        }

        public void VisitVoid(VoidExpression leaf)
        {
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            // TODO: Should this be optional to allow more flexibility with a IBinaryOperationVisitor?
            leaf.LHS = ProcessReplacement(leaf.LHS);
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.SetArgument(i, ProcessReplacement(leaf.GetArgument(i)));
            }
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
            // TODO: Should we do anything here?
        }

        private IExpressionLeaf ProcessReplacement(IExpressionLeaf leaf)
        {
            _replacer.PrepareToVisit();
            leaf.AcceptExpressionVisitor(_replacer);

            if (!_replacer.ShouldReplace) {
                return leaf;
            }

            return _replacer.NewLeaf;
        }
    }
}
