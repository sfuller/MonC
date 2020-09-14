using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class ExpressionChildrenVisitor : IExpressionVisitor
    {
        public IExpressionVisitor Visitor;

        public ExpressionChildrenVisitor(IExpressionVisitor visitor)
        {
            Visitor = visitor;
        }

        public ExpressionChildrenVisitor SetVisitor(IExpressionVisitor visitor)
        {
            Visitor = visitor;
            return this;
        }

        public void VisitVoid(VoidExpression leaf)
        {
            Visitor.VisitVoid(leaf);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            Visitor.VisitNumericLiteral(leaf);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            Visitor.VisitStringLiteral(leaf);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            Visitor.VisitEnumValue(leaf);
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            Visitor.VisitVariable(leaf);
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            Visitor.VisitUnaryOperation(leaf);
            leaf.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            Visitor.VisitBinaryOperation(leaf);

            // NOTE: We may want to make this recursion of LHS and RHS optional, in case the outer visitor uses a
            // IBinaryOperationVisitor.
            leaf.LHS.AcceptExpressionVisitor(this);
            leaf.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            Visitor.VisitFunctionCall(leaf);
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                IExpressionLeaf argument = leaf.GetArgument(i);
                argument.AcceptExpressionVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            Visitor.VisitAssignment(leaf);
            leaf.RHS.AcceptExpressionVisitor(this);
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
            Visitor.VisitUnknown(leaf);
        }
    }
}
