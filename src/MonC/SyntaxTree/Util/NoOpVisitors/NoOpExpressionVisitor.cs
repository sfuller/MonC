using System;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;

namespace MonC.SyntaxTree.Util.NoOpVisitors
{
    public class NoOpExpressionVisitor : IExpressionVisitor, IBinaryOperationVisitor
    {
        public void VisitVoid(VoidExpression leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitEnumValue(EnumValueLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitVariable(VariableLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitAssignment(AssignmentLeaf leaf)
        {
            VisitDefaultExpression(leaf);
        }

        public virtual void VisitUnknown(IExpressionLeaf leaf)
        {
            ThrowForUnknown();
        }

        protected void ThrowForUnknown()
        {
            throw new InvalidOperationException("Don't know how to handle this kind of leaf");
        }

        protected virtual void VisitDefaultExpression(IExpressionLeaf leaf)
        {
        }

        public virtual void VisitCompareLTBinOp(CompareLTBinOpLeaf leaf)
        {
        }

        public virtual void VisitCompareLTEBinOp(CompareLTEBinOpLeaf leaf)
        {
        }

        public virtual void VisitCompareGTBinOp(CompareGTBinOpLeaf leaf)
        {
        }

        public virtual void VisitCompareGTEBinOp(CompareGTEBinOpLeaf leaf)
        {
        }

        public virtual void VisitCompareEqualityBinOp(CompareEqualityBinOpLeaf leaf)
        {
        }

        public virtual void VisitCompareInequalityBinOp(CompareInequalityBinOpLeaf leaf)
        {
        }

        public virtual void VisitLogicalAndBinOp(LogicalAndBinOpLeaf leaf)
        {
        }

        public virtual void VisitLogicalOrBinOp(LogicalOrBinOpLeaf leaf)
        {
        }

        public virtual void VisitAddBinOp(AddBinOpLeaf leaf)
        {
        }

        public virtual void VisitSubtractBinOp(SubtractBinOpLeaf leaf)
        {
        }

        public virtual void VisitMultiplyBinOp(MultiplyBinOpLeaf leaf)
        {
        }

        public virtual void VisitDivideBinOp(DivideBinOpLeaf leaf)
        {
        }

        public virtual void VisitModuloBinOp(ModuloBinOpLeaf leaf)
        {
        }

        public virtual void VisitUnknown(IBinaryOperationLeaf leaf)
        {
            ThrowForUnknown();
        }
    }
}
