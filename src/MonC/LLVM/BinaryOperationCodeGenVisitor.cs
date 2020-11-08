using System;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.LLVM
{
    public readonly struct BinaryOperationCodeGenVisitor : IBinaryOperationVisitor
    {
        private readonly FunctionCodeGenVisitor _codeGenVisitor;

        public BinaryOperationCodeGenVisitor(FunctionCodeGenVisitor codeGenVisitor) =>
            _codeGenVisitor = codeGenVisitor;

        public void VisitCompareLTBinOp(CompareLtBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntSLT, CAPI.LLVMRealPredicate.RealOLT);

        public void VisitCompareLTEBinOp(CompareLteBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntSLE, CAPI.LLVMRealPredicate.RealOLE);

        public void VisitCompareGTBinOp(CompareGtBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntSGT, CAPI.LLVMRealPredicate.RealOGT);

        public void VisitCompareGTEBinOp(CompareGteBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntSGE, CAPI.LLVMRealPredicate.RealOGE);

        public void VisitCompareEqualityBinOp(CompareEqualityBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntEQ, CAPI.LLVMRealPredicate.RealUEQ);

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node) =>
            GenerateRelationalComparison(node, CAPI.LLVMIntPredicate.IntNE, CAPI.LLVMRealPredicate.RealUNE);

        public void VisitLogicalAndBinOp(LogicalAndBinOpNode node)
        {
            node.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            lhs = _codeGenVisitor.ConvertToBool(lhs);

            BasicBlock contBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("land.end");
            BasicBlock rhsBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("land.rhs");

            _codeGenVisitor._builder.BuildCondBr(lhs, rhsBlock, contBlock);
            BasicBlock lhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _codeGenVisitor._builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            node.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            rhs = _codeGenVisitor.ConvertToBool(rhs);
            _codeGenVisitor._builder.BuildBr(contBlock);
            BasicBlock rhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            // A phi instruction is used to generate a boolean false if the LHS' branch is taken
            // Otherwise, the RHS value is used
            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(contBlock);
            _codeGenVisitor._builder.PositionAtEnd(contBlock);
            Value phi = _codeGenVisitor._builder.BuildPhi(_codeGenVisitor._genContext.Context.Int1Type);
            phi.AddIncoming(new[] {Value.ConstInt(_codeGenVisitor._genContext.Context.Int1Type, 0, false), rhs},
                new[] {lhsPredBlock, rhsPredBlock});

            _codeGenVisitor._visitedValue = phi;
        }

        public void VisitLogicalOrBinOp(LogicalOrBinOpNode node)
        {
            node.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            lhs = _codeGenVisitor.ConvertToBool(lhs);

            BasicBlock contBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("lor.end");
            BasicBlock rhsBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("lor.rhs");

            _codeGenVisitor._builder.BuildCondBr(lhs, contBlock, rhsBlock);
            BasicBlock lhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _codeGenVisitor._builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            node.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            rhs = _codeGenVisitor.ConvertToBool(rhs);
            _codeGenVisitor._builder.BuildBr(contBlock);
            BasicBlock rhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            // A phi instruction is used to generate a boolean true if the LHS' branch is taken
            // Otherwise, the RHS value is used
            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(contBlock);
            _codeGenVisitor._builder.PositionAtEnd(contBlock);
            Value phi = _codeGenVisitor._builder.BuildPhi(_codeGenVisitor._genContext.Context.Int1Type);
            phi.AddIncoming(new[] {Value.ConstInt(_codeGenVisitor._genContext.Context.Int1Type, 1, false), rhs},
                new[] {lhsPredBlock, rhsPredBlock});

            _codeGenVisitor._visitedValue = phi;
        }

        public void VisitAddBinOp(AddBinOpNode node)
        {
            GetBinaryArithmeticOperands(node, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFAdd(lhs, rhs)
                : _codeGenVisitor._builder.BuildAdd(lhs, rhs);
        }

        public void VisitSubtractBinOp(SubtractBinOpNode node)
        {
            GetBinaryArithmeticOperands(node, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFSub(lhs, rhs)
                : _codeGenVisitor._builder.BuildSub(lhs, rhs);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpNode node)
        {
            GetBinaryArithmeticOperands(node, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFMul(lhs, rhs)
                : _codeGenVisitor._builder.BuildMul(lhs, rhs);
        }

        public void VisitDivideBinOp(DivideBinOpNode node)
        {
            GetBinaryArithmeticOperands(node, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFDiv(lhs, rhs)
                : _codeGenVisitor._builder.BuildSDiv(lhs, rhs);
        }

        public void VisitModuloBinOp(ModuloBinOpNode node)
        {
            GetBinaryArithmeticOperands(node, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFRem(lhs, rhs)
                : _codeGenVisitor._builder.BuildSRem(lhs, rhs);
        }

        public void VisitUnknown(IBinaryOperationNode node)
        {
            throw new InvalidOperationException(
                "Unexpected binary operation node type. Was replacement of a parse tree node missed?");
        }

        private void TypePromotionForBinaryOperation(ref Value lhs, ref Value rhs, out bool isFloat)
        {
            Type lhsTp = lhs.TypeOf;
            Type rhsTp = rhs.TypeOf;

            if (lhsTp == rhsTp) {
                isFloat = lhsTp.IsFloatingPointType();
                return;
            }

            // TODO: support unsigned values
            CAPI.LLVMOpcode lhsCastOp = _codeGenVisitor.GetCastOpcode(lhs, rhsTp);
            if (lhsCastOp != CAPI.LLVMOpcode.Trunc && lhsCastOp != CAPI.LLVMOpcode.FPTrunc) {
                lhs = _codeGenVisitor._builder.BuildCast(lhsCastOp, lhs, rhsTp);
                isFloat = rhsTp.IsFloatingPointType();
                return;
            }

            // TODO: support unsigned values
            CAPI.LLVMOpcode rhsCastOp = _codeGenVisitor.GetCastOpcode(rhs, lhsTp);
            rhs = _codeGenVisitor._builder.BuildCast(rhsCastOp, rhs, lhsTp);
            isFloat = lhsTp.IsFloatingPointType();
        }

        private void GenerateRelationalComparison(IBinaryOperationNode node, CAPI.LLVMIntPredicate intPred,
            CAPI.LLVMRealPredicate realPred)
        {
            node.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            node.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out bool isFloat);

            if (!isFloat) {
                // TODO: Support unsigned values
                _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildICmp(intPred, lhs, rhs, "cmp");
            } else {
                _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildFCmp(realPred, lhs, rhs, "cmp");
            }
        }

        private void GetBinaryArithmeticOperands(IBinaryOperationNode node, out Value lhs, out Value rhs,
            out bool isFloat)
        {
            node.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            node.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(node);
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out isFloat);
        }
    }
}
