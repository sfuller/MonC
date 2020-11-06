using System;
using MonC.IL;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.Codegen
{
    public class BinaryOperationCodeGenVisitor : IBinaryOperationVisitor
    {
        private readonly IExpressionVisitor _expressionVisitor;
        private readonly FunctionBuilder _functionBuilder;

        public int ComparisonOperationAddress { get; private set; }

        public BinaryOperationCodeGenVisitor(IExpressionVisitor expressionVisitor, FunctionBuilder functionBuilder)
        {
            _expressionVisitor = expressionVisitor;
            _functionBuilder = functionBuilder;
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
            BasicComparison(node, OpCode.CMPE);
        }

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            node.LHS.AcceptExpressionVisitor(_expressionVisitor);
            ComparisonOperationAddress = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(OpCode.CMPE);
            _functionBuilder.AddInstruction(OpCode.LNOT);
        }

        public void VisitLogicalAndBinOp(LogicalAndBinOpNode node)
        {
            node.LHS.AcceptExpressionVisitor(_expressionVisitor);
            _functionBuilder.AddInstruction(OpCode.BOOL);
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            ComparisonOperationAddress = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(OpCode.BOOL);
            _functionBuilder.AddInstruction(OpCode.AND);
        }

        public void VisitLogicalOrBinOp(LogicalOrBinOpNode node)
        {
            node.LHS.AcceptExpressionVisitor(_expressionVisitor);
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            ComparisonOperationAddress = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(OpCode.OR);
            _functionBuilder.AddInstruction(OpCode.BOOL);
        }

        public void VisitAddBinOp(AddBinOpNode node)
        {
            BasicComparison(node, OpCode.ADD);
        }

        public void VisitSubtractBinOp(SubtractBinOpNode node)
        {
            BasicComparison(node, OpCode.SUB);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpNode node)
        {
            BasicComparison(node, OpCode.MUL);
        }

        public void VisitDivideBinOp(DivideBinOpNode node)
        {
            BasicComparison(node, OpCode.DIV);
        }

        public void VisitModuloBinOp(ModuloBinOpNode node)
        {
            BasicComparison(node, OpCode.MOD);
        }

        public void VisitUnknown(IBinaryOperationNode node)
        {
            throw new InvalidOperationException("Unexpected binary operation node type. Was replacement of a parse tree node missed?");
        }

        private void BasicComparison(IBinaryOperationNode node, OpCode opCode)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            node.LHS.AcceptExpressionVisitor(_expressionVisitor);

            ComparisonOperationAddress = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(opCode);
        }

        private void GenerateRelationalComparison(IBinaryOperationNode node)
        {
            node.RHS.AcceptExpressionVisitor(_expressionVisitor);
            node.LHS.AcceptExpressionVisitor(_expressionVisitor);
            ComparisonOperationAddress = _functionBuilder.InstructionCount;

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
