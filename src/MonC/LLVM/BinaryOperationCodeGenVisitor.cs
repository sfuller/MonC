using System;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;

namespace MonC.LLVM
{
    public struct BinaryOperationCodeGenVisitor : IBinaryOperationVisitor
    {
        private FunctionCodeGenVisitor _codeGenVisitor;

        public BinaryOperationCodeGenVisitor(FunctionCodeGenVisitor codeGenVisitor) =>
            _codeGenVisitor = codeGenVisitor;

        public void VisitCompareLTBinOp(CompareLTBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntSLT, CAPI.LLVMRealPredicate.RealOLT);

        public void VisitCompareLTEBinOp(CompareLTEBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntSLE, CAPI.LLVMRealPredicate.RealOLE);

        public void VisitCompareGTBinOp(CompareGTBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntSGT, CAPI.LLVMRealPredicate.RealOGT);

        public void VisitCompareGTEBinOp(CompareGTEBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntSGE, CAPI.LLVMRealPredicate.RealOGE);

        public void VisitCompareEqualityBinOp(CompareEqualityBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntEQ, CAPI.LLVMRealPredicate.RealUEQ);

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpLeaf leaf) =>
            GenerateRelationalComparison(leaf, CAPI.LLVMIntPredicate.IntNE, CAPI.LLVMRealPredicate.RealUNE);

        public void VisitLogicalAndBinOp(LogicalAndBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
            lhs = _codeGenVisitor.ConvertToBool(lhs);

            BasicBlock contBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("land.end");
            BasicBlock rhsBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("land.rhs");

            _codeGenVisitor._builder.BuildCondBr(lhs, rhsBlock, contBlock);
            BasicBlock lhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _codeGenVisitor._builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            leaf.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
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

        public void VisitLogicalOrBinOp(LogicalOrBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
            lhs = _codeGenVisitor.ConvertToBool(lhs);

            BasicBlock contBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("lor.end");
            BasicBlock rhsBlock = _codeGenVisitor._genContext.Context.CreateBasicBlock("lor.rhs");

            _codeGenVisitor._builder.BuildCondBr(lhs, contBlock, rhsBlock);
            BasicBlock lhsPredBlock = _codeGenVisitor._builder.InsertBlock;

            _codeGenVisitor._builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _codeGenVisitor._builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            leaf.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
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

        public void VisitAddBinOp(AddBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            GetBinaryArithmeticOperands(leaf, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFAdd(lhs, rhs)
                : _codeGenVisitor._builder.BuildAdd(lhs, rhs);
        }

        public void VisitSubtractBinOp(SubtractBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            GetBinaryArithmeticOperands(leaf, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFSub(lhs, rhs)
                : _codeGenVisitor._builder.BuildSub(lhs, rhs);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            GetBinaryArithmeticOperands(leaf, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFMul(lhs, rhs)
                : _codeGenVisitor._builder.BuildMul(lhs, rhs);
        }

        public void VisitDivideBinOp(DivideBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            GetBinaryArithmeticOperands(leaf, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFDiv(lhs, rhs)
                : _codeGenVisitor._builder.BuildSDiv(lhs, rhs);
        }

        public void VisitModuloBinOp(ModuloBinOpLeaf leaf)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            GetBinaryArithmeticOperands(leaf, out Value lhs, out Value rhs, out bool isFloat);
            _codeGenVisitor._visitedValue = isFloat
                ? _codeGenVisitor._builder.BuildFRem(lhs, rhs)
                : _codeGenVisitor._builder.BuildSRem(lhs, rhs);
        }

        public void VisitUnknown(IBinaryOperationLeaf leaf)
        {
            throw new InvalidOperationException(
                "Unexpected binary operation leaf type. Was replacement of a parse tree leaf missed?");
        }

        private void TypePromotionForBinaryOperation(ref Value lhs, ref Value rhs, out bool isFloat)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

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

        private void GenerateRelationalComparison(IBinaryOperationLeaf leaf, CAPI.LLVMIntPredicate intPred,
            CAPI.LLVMRealPredicate realPred)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            leaf.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            Value rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out bool isFloat);

            if (!isFloat) {
                // TODO: Support unsigned values
                _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildICmp(intPred, lhs, rhs, "cmp");
            } else {
                _codeGenVisitor._visitedValue = _codeGenVisitor._builder.BuildFCmp(realPred, lhs, rhs, "cmp");
            }
        }

        private void GetBinaryArithmeticOperands(IBinaryOperationLeaf leaf, out Value lhs, out Value rhs,
            out bool isFloat)
        {
            if (_codeGenVisitor._builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.AcceptExpressionVisitor(_codeGenVisitor);
            lhs = _codeGenVisitor._visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            leaf.RHS.AcceptExpressionVisitor(_codeGenVisitor);
            rhs = _codeGenVisitor._visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            _codeGenVisitor.SetCurrentDebugLocation(leaf);
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out isFloat);
        }
    }
}
