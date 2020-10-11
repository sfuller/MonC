using System;
using MonC.IL;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.Codegen
{
    public class BinaryOperationCodeGenVisitor : IBinaryOperationVisitor
    {
        private readonly FunctionBuilder _functionBuilder;

        //private int _rhsStackAddress;

        public BinaryOperationCodeGenVisitor(FunctionBuilder functionBuilder)
        {
            _functionBuilder = functionBuilder;
        }

        public void Setup()
        {
            // Note: LHS value is not in stack, but in current register.
            //_rhsStackAddress = rhsStackAddress;
        }

        public void VisitCompareLTBinOp(CompareLtBinOpNode node)
        {
            GenerateRelationalComparison(node);
        }

        public void VisitCompareLTEBinOp(CompareLteBinOpNode node)
        {
            GenerateRelationalComparison(node);
        }

        public void VisitCompareGTBinOp(CompareGtBinOpNode node)
        {
            GenerateRelationalComparison(node);
        }

        public void VisitCompareGTEBinOp(CompareGteBinOpNode node)
        {
            GenerateRelationalComparison(node);
        }

        public void VisitCompareEqualityBinOp(CompareEqualityBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.CMPE);
        }

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.CMPE);
            _functionBuilder.AddInstruction(OpCode.LNOT);
        }

        public void VisitLogicalAndBinOp(LogicalAndBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.BOOL);
            _functionBuilder.AddInstruction(OpCode.AND);
        }

        public void VisitLogicalOrBinOp(LogicalOrBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.OR);
            _functionBuilder.AddInstruction(OpCode.BOOL);
        }

        public void VisitAddBinOp(AddBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.ADD);
        }

        public void VisitSubtractBinOp(SubtractBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.SUB);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.MUL);
        }

        public void VisitDivideBinOp(DivideBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.DIV);
        }

        public void VisitModuloBinOp(ModuloBinOpNode node)
        {
            _functionBuilder.AddInstruction(OpCode.MOD);
        }

        public void VisitUnknown(IBinaryOperationNode node)
        {
            throw new InvalidOperationException("Unexpected binary operation node type. Was replacement of a parse tree node missed?");
        }

        private void GenerateRelationalComparison(IBinaryOperationNode node)
        {
            bool isGreaterThan = node is CompareGtBinOpNode || node is CompareGteBinOpNode;
            bool includeEquals = (node is CompareLteBinOpNode || node is CompareGteBinOpNode) ^ isGreaterThan;

            if (includeEquals) {
                _functionBuilder.AddInstruction(OpCode.CMPLTE);
            } else {
                _functionBuilder.AddInstruction(OpCode.CMPLT);
            }

            if (isGreaterThan) {
                _functionBuilder.AddInstruction(OpCode.LNOT);
            }
        }
    }
}
