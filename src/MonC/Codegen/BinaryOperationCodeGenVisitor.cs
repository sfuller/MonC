using System;
using MonC.IL;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;

namespace MonC.Codegen
{
    public class BinaryOperationCodeGenVisitor : IBinaryOperationVisitor
    {
        private readonly FunctionBuilder _functionBuilder;

        private int _rhsStackAddress;

        public BinaryOperationCodeGenVisitor(FunctionBuilder functionBuilder)
        {
            _functionBuilder = functionBuilder;
        }

        public void Setup(int rhsStackAddress)
        {
            // Note: LHS value is not in stack, but in current register.
            _rhsStackAddress = rhsStackAddress;
        }

        public void VisitCompareLTBinOp(CompareLTBinOpLeaf leaf)
        {
            GenerateRelationalComparison(leaf);
        }

        public void VisitCompareLTEBinOp(CompareLTEBinOpLeaf leaf)
        {
            GenerateRelationalComparison(leaf);
        }

        public void VisitCompareGTBinOp(CompareGTBinOpLeaf leaf)
        {
            GenerateRelationalComparison(leaf);
        }

        public void VisitCompareGTEBinOp(CompareGTEBinOpLeaf leaf)
        {
            GenerateRelationalComparison(leaf);
        }

        public void VisitCompareEqualityBinOp(CompareEqualityBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.CMPE, _rhsStackAddress);
        }

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.CMPE, _rhsStackAddress);
            _functionBuilder.AddInstruction(OpCode.LNOT);
        }

        public void VisitLogicalAndBinOp(LogicalAndBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.BOOL);
            _functionBuilder.AddInstruction(OpCode.AND, _rhsStackAddress);
        }

        public void VisitLogicalOrBinOp(LogicalOrBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.OR, _rhsStackAddress);
            _functionBuilder.AddInstruction(OpCode.BOOL);
        }

        public void VisitAddBinOp(AddBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.ADD, _rhsStackAddress);
        }

        public void VisitSubtractBinOp(SubtractBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.SUB, _rhsStackAddress);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.MUL, _rhsStackAddress);
        }

        public void VisitDivideBinOp(DivideBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.DIV, _rhsStackAddress);
        }

        public void VisitModuloBinOp(ModuloBinOpLeaf leaf)
        {
            _functionBuilder.AddInstruction(OpCode.MOD, _rhsStackAddress);
        }

        public void VisitUnknown(IBinaryOperationLeaf leaf)
        {
            throw new InvalidOperationException("Unexpected binary operation leaf type. Was replacement of a parse tree leaf missed?");
        }

        private void GenerateRelationalComparison(IBinaryOperationLeaf leaf)
        {
            bool isGreaterThan = leaf is CompareGTBinOpLeaf || leaf is CompareGTEBinOpLeaf;
            bool includeEquals = (leaf is CompareLTEBinOpLeaf || leaf is CompareGTEBinOpLeaf) ^ isGreaterThan;

            if (includeEquals) {
                _functionBuilder.AddInstruction(OpCode.CMPLTE, _rhsStackAddress);
            } else {
                _functionBuilder.AddInstruction(OpCode.CMPLT, _rhsStackAddress);
            }

            if (isGreaterThan) {
                _functionBuilder.AddInstruction(OpCode.LNOT);
            }
        }
    }
}
