using System;
using MonC.SyntaxTree;

namespace MonC.LLVM
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
        private CodeGeneratorContext _genContext;
        private CodeGeneratorContext.Function _function;
        private Builder? _builder;
        private BasicBlock _basicBlock;
        private Metadata _lexicalScope;
        private Value _visitedValue;

        internal CodeGenVisitor(CodeGeneratorContext genContext, CodeGeneratorContext.Function function)
        {
            _genContext = genContext;
            _function = function;
        }

        private Metadata SetCurrentDebugLocation(IASTLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf, out Symbol range);
                Metadata location = _genContext.Context.CreateDebugLocation(range.LLVMLine, range.LLVMColumn,
                    _lexicalScope, Metadata.Null);
                _builder.SetCurrentDebugLocation(location);
                return location;
            }
            
            return Metadata.Null;
        }

        private void BuildBrIfNecessary(BasicBlock dest)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            if (_builder.GetInsertBlock().LastInstruction.InstructionOpcode != CAPI.LLVMOpcode.Br) {
                _builder.BuildBr(dest);
            }
        }

        private Value ConvertToBool(Value val, bool invert = false)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            Type valType = val.TypeOf;
            if (valType.Kind != CAPI.LLVMTypeKind.Integer) {
                throw new InvalidOperationException("only integers can be converted to bool");
            }

            if (valType.IntTypeWidth == 1) {
                return val;
            }

            return _builder.BuildICmp(invert ? CAPI.LLVMIntPredicate.IntEQ : CAPI.LLVMIntPredicate.IntNE, val,
                Value.ConstInt(valType, 0, true), "tobool");
        }

        private Value GenerateRelationalComparison(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.Accept(this);
            Value lhs = _visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            leaf.RHS.Accept(this);
            Value rhs = _visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            CAPI.LLVMIntPredicate opPred;
            switch (leaf.Op.Value) {
                case ">":
                    opPred = CAPI.LLVMIntPredicate.IntSGT;
                    break;
                case "<":
                    opPred = CAPI.LLVMIntPredicate.IntSLT;
                    break;
                case Syntax.GREATER_THAN_OR_EQUAL_TO:
                    opPred = CAPI.LLVMIntPredicate.IntSGE;
                    break;
                case Syntax.LESS_THAN_OR_EQUAL_TO:
                    opPred = CAPI.LLVMIntPredicate.IntSLE;
                    break;
                case Syntax.EQUALS:
                    opPred = CAPI.LLVMIntPredicate.IntEQ;
                    break;
                case Syntax.NOT_EQUALS:
                    opPred = CAPI.LLVMIntPredicate.IntNE;
                    break;
                default:
                    throw new NotImplementedException(leaf.Op.Value);
            }

            SetCurrentDebugLocation(leaf);
            return _builder.BuildICmp(opPred, lhs, rhs, "cmp");
        }

        private Value GenerateLogicalOr(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.Accept(this);
            Value lhs = _visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            lhs = ConvertToBool(lhs);

            BasicBlock contBlock = _genContext.Context.CreateBasicBlock("lor.end");
            BasicBlock rhsBlock = _genContext.Context.CreateBasicBlock("lor.rhs");

            _builder.BuildCondBr(lhs, contBlock, rhsBlock);
            BasicBlock lhsPredBlock = _builder.GetInsertBlock();

            _builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            leaf.RHS.Accept(this);
            Value rhs = _visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            rhs = ConvertToBool(rhs);
            _builder.BuildBr(contBlock);
            BasicBlock rhsPredBlock = _builder.GetInsertBlock();

            // A phi instruction is used to generate a boolean true if the LHS' branch is taken
            // Otherwise, the RHS value is used
            _builder.InsertExistingBasicBlockAfterInsertBlock(contBlock);
            _builder.PositionAtEnd(contBlock);
            Value phi = _builder.BuildPhi(_genContext.Context.Int1Type);
            phi.AddIncoming(new Value[] {Value.ConstInt(_genContext.Context.Int1Type, 1, false), rhs},
                new BasicBlock[] {lhsPredBlock, rhsPredBlock});

            return phi;
        }

        private Value GenerateLogicalAnd(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.Accept(this);
            Value lhs = _visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            lhs = ConvertToBool(lhs);

            BasicBlock contBlock = _genContext.Context.CreateBasicBlock("land.end");
            BasicBlock rhsBlock = _genContext.Context.CreateBasicBlock("land.rhs");

            _builder.BuildCondBr(lhs, rhsBlock, contBlock);
            BasicBlock lhsPredBlock = _builder.GetInsertBlock();

            _builder.InsertExistingBasicBlockAfterInsertBlock(rhsBlock);
            _builder.PositionAtEnd(rhsBlock);

            // TODO: This can be further optimized by recursively gathering LHS pred blocks and adding them to the phi
            // rather than generating several phis
            leaf.RHS.Accept(this);
            Value rhs = _visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            rhs = ConvertToBool(rhs);
            BasicBlock rhsPredBlock = _builder.GetInsertBlock();

            // A phi instruction is used to generate a boolean false if the LHS' branch is taken
            // Otherwise, the RHS value is used
            _builder.InsertExistingBasicBlockAfterInsertBlock(contBlock);
            _builder.PositionAtEnd(contBlock);
            Value phi = _builder.BuildPhi(_genContext.Context.Int1Type);
            phi.AddIncoming(new Value[] {Value.ConstInt(_genContext.Context.Int1Type, 0, false), rhs},
                new BasicBlock[] {lhsPredBlock, rhsPredBlock});

            return phi;
        }

        private Value GenerateBinaryArithmetic(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.LHS.Accept(this);
            Value lhs = _visitedValue;
            if (!lhs.IsValid) {
                throw new InvalidOperationException("LHS did not produce a usable rvalue");
            }

            leaf.RHS.Accept(this);
            Value rhs = _visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            switch (leaf.Op.Value) {
                case "+":
                    return _builder.BuildAdd(lhs, rhs);
                case "-":
                    return _builder.BuildSub(lhs, rhs);
                case "*":
                    return _builder.BuildMul(lhs, rhs);
                case "/":
                    return _builder.BuildSDiv(lhs, rhs);
                case "%":
                    return _builder.BuildSRem(lhs, rhs);
                default:
                    throw new NotImplementedException(leaf.Op.Value);
            }
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            switch (leaf.Op.Value) {
                case ">":
                case "<":
                case Syntax.GREATER_THAN_OR_EQUAL_TO:
                case Syntax.LESS_THAN_OR_EQUAL_TO:
                case Syntax.EQUALS:
                case Syntax.NOT_EQUALS:
                    _visitedValue = GenerateRelationalComparison(leaf);
                    break;
                case Syntax.LOGICAL_OR:
                    _visitedValue = GenerateLogicalOr(leaf);
                    break;
                case Syntax.LOGICAL_AND:
                    _visitedValue = GenerateLogicalAnd(leaf);
                    break;
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                    _visitedValue = GenerateBinaryArithmetic(leaf);
                    break;
                default:
                    throw new NotImplementedException(leaf.Op.Value);
            }
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.RHS.Accept(this);
            Value rhs = _visitedValue;
            if (!rhs.IsValid) {
                throw new InvalidOperationException("RHS did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            switch (leaf.Operator.Value) {
                case "-":
                    _visitedValue = _builder.BuildNeg(rhs);
                    break;
                case "!":
                    _visitedValue = ConvertToBool(rhs, true);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IASTLeaf statement = leaf.GetStatement(i);
                statement.Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            IASTLeaf? assignment = leaf.Assignment;
            if (assignment == null) {
                return;
            }

            assignment.Accept(this);
            if (!_visitedValue.IsValid) {
                throw new InvalidOperationException("assignment did not produce a usable rvalue");
            }

            Metadata dbgLocation = SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf, out Value varStorage);

            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf, out Symbol varRange);
                Metadata varType = _genContext.LookupDiType(leaf.Type);
                Metadata varMetadata = _genContext.DiBuilder.CreateAutoVariable(_function.DiFunctionDef, leaf.Name,
                    _genContext.DiFile, varRange.LLVMLine, varType, true, CAPI.DI.LLVMDIFlags.Zero,
                    varType.TypeAlignInBits);
                _genContext.DiBuilder.InsertDeclareAtEnd(varStorage, varMetadata,
                    _genContext.DiBuilder.CreateExpression(), dbgLocation, _builder.GetInsertBlock());
            }

            _visitedValue = _builder.BuildStore(_visitedValue, varStorage);
        }

        public void VisitFor(ForLeaf leaf)
        {
        }

        private bool _hasVisitedFunctionDefinition;

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            if (_hasVisitedFunctionDefinition) {
                // This should only be called once, for the function definition being generated.
                throw new InvalidOperationException("Attempting to visit another function definition.");
            }

            _hasVisitedFunctionDefinition = true;

            using (Builder builder = _genContext.Context.CreateBuilder()) {
                _builder = builder;
                _basicBlock = _function.StartDefinition(_genContext, _builder);
                _builder.PositionAtEnd(_basicBlock);
                _lexicalScope = _function.DiFunctionDef;
                leaf.Body.Accept(this);
                if (_function.ReturnBlock != null && _function.RetvalStorage != null) {
                    _builder.InsertExistingBasicBlockAfterInsertBlock(_function.ReturnBlock.Value);
                    _builder.PositionAtEnd(_function.ReturnBlock.Value);
                    Value retVal = _builder.BuildLoad(_function.FunctionType.ReturnType, _function.RetvalStorage.Value);
                    _builder.BuildRet(retVal);
                }

                _builder = null;
            }
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildLoad(_genContext.LookupType(leaf.Declaration.Type), varStorage);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.Condition.Accept(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            BasicBlock ifThenBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "if.then");
            BasicBlock ifElseBasicBlock = BasicBlock.Null;
            if (leaf.ElseBody != null)
                ifElseBasicBlock = _genContext.Context.CreateBasicBlock("if.else");
            BasicBlock ifEndBasicBlock = _genContext.Context.CreateBasicBlock("if.end");

            SetCurrentDebugLocation(leaf);
            _builder.BuildCondBr(condVal, ifThenBasicBlock,
                ifElseBasicBlock.IsValid ? ifElseBasicBlock : ifEndBasicBlock);

            _builder.PositionAtEnd(ifThenBasicBlock);
            leaf.IfBody.Accept(this);
            BuildBrIfNecessary(ifEndBasicBlock);
            if (leaf.ElseBody != null) {
                _builder.InsertExistingBasicBlockAfterInsertBlock(ifElseBasicBlock);
                _builder.PositionAtEnd(ifElseBasicBlock);
                leaf.ElseBody.Accept(this);
                BuildBrIfNecessary(ifEndBasicBlock);
            }

            _builder.InsertExistingBasicBlockAfterInsertBlock(ifEndBasicBlock);
            _builder.PositionAtEnd(ifEndBasicBlock);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) leaf.Value, true);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            if (leaf.RHS == null) {
                SetCurrentDebugLocation(leaf);
                if (_function.ReturnBlock != null) {
                    _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
                } else {
                    _visitedValue = _builder.BuildRetVoid();
                }

                return;
            }

            leaf.RHS.Accept(this);
            Value retVal = _visitedValue;
            if (!retVal.IsValid) {
                throw new InvalidOperationException("return did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            if (_function.ReturnBlock != null && _function.RetvalStorage != null) {
                _builder.BuildStore(retVal, _function.RetvalStorage.Value);
                _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
            } else {
                _visitedValue = _builder.BuildRet(retVal);
            }
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            leaf.RHS.Accept(this);
            if (!_visitedValue.IsValid) {
                throw new InvalidOperationException("assignment did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildStore(_visitedValue, varStorage);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
        }
    }
}