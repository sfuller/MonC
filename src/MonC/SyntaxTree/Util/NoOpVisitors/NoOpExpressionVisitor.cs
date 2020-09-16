using System;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.SyntaxTree.Util.NoOpVisitors
{
    public class NoOpExpressionVisitor : IExpressionVisitor, IBinaryOperationVisitor
    {
        public void VisitVoid(VoidExpressionNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitNumericLiteral(NumericLiteralNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitStringLiteral(StringLiteralNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitEnumValue(EnumValueNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitVariable(VariableNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitUnaryOperation(IUnaryOperationNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitBinaryOperation(IBinaryOperationNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitFunctionCall(FunctionCallNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitAssignment(AssignmentNode node)
        {
            VisitDefaultExpression(node);
        }

        public virtual void VisitUnknown(IExpressionNode node)
        {
            ThrowForUnknown();
        }

        protected void ThrowForUnknown()
        {
            throw new InvalidOperationException("Don't know how to handle this kind of node");
        }

        protected virtual void VisitDefaultExpression(IExpressionNode node)
        {
        }

        public virtual void VisitCompareLTBinOp(CompareLtBinOpNode node)
        {
        }

        public virtual void VisitCompareLTEBinOp(CompareLteBinOpNode node)
        {
        }

        public virtual void VisitCompareGTBinOp(CompareGtBinOpNode node)
        {
        }

        public virtual void VisitCompareGTEBinOp(CompareGteBinOpNode node)
        {
        }

        public virtual void VisitCompareEqualityBinOp(CompareEqualityBinOpNode node)
        {
        }

        public virtual void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node)
        {
        }

        public virtual void VisitLogicalAndBinOp(LogicalAndBinOpNode node)
        {
        }

        public virtual void VisitLogicalOrBinOp(LogicalOrBinOpNode node)
        {
        }

        public virtual void VisitAddBinOp(AddBinOpNode node)
        {
        }

        public virtual void VisitSubtractBinOp(SubtractBinOpNode node)
        {
        }

        public virtual void VisitMultiplyBinOp(MultiplyBinOpNode node)
        {
        }

        public virtual void VisitDivideBinOp(DivideBinOpNode node)
        {
        }

        public virtual void VisitModuloBinOp(ModuloBinOpNode node)
        {
        }

        public virtual void VisitUnknown(IBinaryOperationNode node)
        {
            ThrowForUnknown();
        }
    }
}
