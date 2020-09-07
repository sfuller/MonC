using System;
using System.Linq;
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
                Metadata location = _genContext.Context.CreateDebugLocation(range.LLVMLine,
                    _genContext.ColumnInfo ? range.LLVMColumn : 0, _lexicalScope, Metadata.Null);
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

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            _builder.BuildBr(dest);
            _builder.ClearInsertionPosition();
        }

        private Value ConvertToBool(Value val, bool invert = false)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // TODO: Support pointers
            Type valType = val.TypeOf;
            if (valType.Kind == CAPI.LLVMTypeKind.Integer) {
                if (valType.IntTypeWidth == 1) {
                    return val;
                }

                return _builder.BuildICmp(invert ? CAPI.LLVMIntPredicate.IntEQ : CAPI.LLVMIntPredicate.IntNE, val,
                    Value.ConstInt(valType, 0, true), "tobool");
            } else if (valType.IsFloatingPointTy()) {
                return _builder.BuildFCmp(invert ? CAPI.LLVMRealPredicate.RealUEQ : CAPI.LLVMRealPredicate.RealUNE, val,
                    Value.ConstReal(valType, 0.0), "tobool");
            }

            throw new InvalidOperationException("only integers and floating point values can be converted to bool");
        }

        private CAPI.LLVMOpcode GetCastOpcode(Value val, Type tp)
        {
            Type valType = val.TypeOf;

            // TODO: This is a hack to not sign-extend bools; Make a better way to track signedness
            bool sext = valType.Kind == CAPI.LLVMTypeKind.Integer && valType.IntTypeWidth != 1;

            // TODO: support unsigned values
            return Builder.GetCastOpcode(val, sext, tp, true);
        }

        private Value ConvertToType(Value val, Type tp)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            if (val.TypeOf == tp) {
                return val;
            }

            CAPI.LLVMOpcode castOp = GetCastOpcode(val, tp);
            return _builder.BuildCast(castOp, val, tp);
        }

        private void TypePromotionForBinaryOperation(ref Value lhs, ref Value rhs, out bool isFloat)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            Type lhsTp = lhs.TypeOf;
            Type rhsTp = rhs.TypeOf;

            if (lhsTp == rhsTp) {
                isFloat = lhsTp.IsFloatingPointTy();
                return;
            }

            // TODO: support unsigned values
            CAPI.LLVMOpcode lhsCastOp = GetCastOpcode(lhs, rhsTp);
            if (lhsCastOp != CAPI.LLVMOpcode.Trunc && lhsCastOp != CAPI.LLVMOpcode.FPTrunc) {
                lhs = _builder.BuildCast(lhsCastOp, lhs, rhsTp);
                isFloat = rhsTp.IsFloatingPointTy();
                return;
            }

            // TODO: support unsigned values
            CAPI.LLVMOpcode rhsCastOp = GetCastOpcode(rhs, lhsTp);
            rhs = _builder.BuildCast(rhsCastOp, rhs, lhsTp);
            isFloat = lhsTp.IsFloatingPointTy();
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

            SetCurrentDebugLocation(leaf);
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out bool isFloat);

            if (!isFloat) {
                // TODO: Support unsigned values
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

                return _builder.BuildICmp(opPred, lhs, rhs, "cmp");
            } else {
                CAPI.LLVMRealPredicate opPred;
                switch (leaf.Op.Value) {
                    case ">":
                        opPred = CAPI.LLVMRealPredicate.RealOGT;
                        break;
                    case "<":
                        opPred = CAPI.LLVMRealPredicate.RealOLT;
                        break;
                    case Syntax.GREATER_THAN_OR_EQUAL_TO:
                        opPred = CAPI.LLVMRealPredicate.RealOGE;
                        break;
                    case Syntax.LESS_THAN_OR_EQUAL_TO:
                        opPred = CAPI.LLVMRealPredicate.RealOLE;
                        break;
                    case Syntax.EQUALS:
                        opPred = CAPI.LLVMRealPredicate.RealUEQ;
                        break;
                    case Syntax.NOT_EQUALS:
                        opPred = CAPI.LLVMRealPredicate.RealUNE;
                        break;
                    default:
                        throw new NotImplementedException(leaf.Op.Value);
                }

                return _builder.BuildFCmp(opPred, lhs, rhs, "cmp");
            }
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
            BasicBlock lhsPredBlock = _builder.InsertBlock;

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
            BasicBlock rhsPredBlock = _builder.InsertBlock;

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
            BasicBlock lhsPredBlock = _builder.InsertBlock;

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
            BasicBlock rhsPredBlock = _builder.InsertBlock;

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
            TypePromotionForBinaryOperation(ref lhs, ref rhs, out bool isFloat);
            if (!isFloat) {
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
            } else {
                switch (leaf.Op.Value) {
                    case "+":
                        return _builder.BuildFAdd(lhs, rhs);
                    case "-":
                        return _builder.BuildFSub(lhs, rhs);
                    case "*":
                        return _builder.BuildFMul(lhs, rhs);
                    case "/":
                        return _builder.BuildFDiv(lhs, rhs);
                    case "%":
                        return _builder.BuildFRem(lhs, rhs);
                    default:
                        throw new NotImplementedException(leaf.Op.Value);
                }
            }
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

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

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

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
            Metadata oldLexicalScope = _lexicalScope;

            if (_lexicalScope.IsValid) {
                // Push new lexical block
                if (_genContext.DiBuilder != null) {
                    _genContext.TryGetTokenSymbol(leaf, out Symbol range);
                    _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                        range.LLVMLine, _genContext.ColumnInfo ? range.LLVMColumn : 0);
                }
            } else {
                // Bootstrap lexical scope with function
                _lexicalScope = _function.DiFunctionDef;
            }

            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IASTLeaf statement = leaf.GetStatement(i);
                statement.Accept(this);
            }

            _lexicalScope = oldLexicalScope;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

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
                Metadata varMetadata = _genContext.DiBuilder.CreateAutoVariable(_lexicalScope, leaf.Name,
                    _genContext.DiFile, varRange.LLVMLine, varType, true, CAPI.LLVMDIFlags.Zero,
                    varType.TypeAlignInBits);
                _genContext.DiBuilder.InsertDeclareAtEnd(varStorage, varMetadata,
                    _genContext.DiBuilder.CreateExpression(), dbgLocation, _builder.InsertBlock);
            }

            _visitedValue = _builder.BuildStore(_visitedValue, varStorage);
        }

        private struct BreakContinue
        {
            public BasicBlock BreakBlock { get; }
            public BasicBlock ContinueBlock { get; }

            public BreakContinue(BasicBlock b, BasicBlock c)
            {
                BreakBlock = b;
                ContinueBlock = c;
            }
        }

        private BreakContinue _breakContinueTop;

        public void VisitFor(ForLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf.Declaration, out Symbol declRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    declRange.LLVMLine, _genContext.ColumnInfo ? declRange.LLVMColumn : 0);
            }

            leaf.Declaration.Accept(this);

            BasicBlock condBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.cond");
            BasicBlock bodyBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.body");
            BasicBlock incBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.inc");
            BasicBlock endBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.end");

            SetCurrentDebugLocation(leaf);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(condBasicBlock);
            leaf.Condition.Accept(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            condVal = ConvertToBool(condVal);

            _builder.BuildCondBr(condVal, bodyBasicBlock, endBasicBlock);

            _builder.PositionAtEnd(bodyBasicBlock);
            BreakContinue oldBreakContinueTop = _breakContinueTop;
            _breakContinueTop = new BreakContinue(endBasicBlock, incBasicBlock);
            leaf.Body.Accept(this);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(incBasicBlock);

            _builder.PositionAtEnd(incBasicBlock);
            leaf.Update.Accept(this);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
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
                leaf.Body.Accept(this);
                if (_function.ReturnBlock != null && _function.RetvalStorage != null) {
                    _function.FunctionValue.AppendExistingBasicBlock(_function.ReturnBlock.Value);
                    _builder.PositionAtEnd(_function.ReturnBlock.Value);

                    if (_genContext.DiBuilder != null) {
                        _genContext.TryGetTokenSymbol(leaf.Body, out Symbol range);
                        Metadata location = _genContext.Context.CreateDebugLocation(range.End.Line + 1,
                            _genContext.ColumnInfo ? range.End.Column + 1 : 0, _function.DiFunctionDef, Metadata.Null);
                        _builder.SetCurrentDebugLocation(location);
                    }

                    Value retVal = _builder.BuildLoad(_function.FunctionType.ReturnType, _function.RetvalStorage.Value);
                    _builder.BuildRet(retVal);
                }

                _builder = null;
            }
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            CodeGeneratorContext.Function func = _genContext.GetFunctionDeclaration(leaf.LHS);
            Type[] paramTypes = func.FunctionType.ParamTypes;

            Value[] args = new Value[leaf.ArgumentCount];
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
                if (!_visitedValue.IsValid) {
                    throw new InvalidOperationException("argument did not produce a usable rvalue");
                }

                args[i] = ConvertToType(_visitedValue, paramTypes[i]);
            }

            SetCurrentDebugLocation(leaf);
            _visitedValue = _builder.BuildCall(func.FunctionType, func.FunctionValue, args);
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildLoad(_genContext.LookupType(leaf.Declaration.Type), varStorage);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf.Condition, out Symbol condRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    condRange.LLVMLine, _genContext.ColumnInfo ? condRange.LLVMColumn : 0);
            }

            leaf.Condition.Accept(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            condVal = ConvertToBool(condVal);

            BasicBlock ifThenBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "if.then");
            BasicBlock ifElseBasicBlock = BasicBlock.Null;
            if (leaf.ElseBody != null)
                ifElseBasicBlock = _genContext.Context.CreateBasicBlock("if.else");
            BasicBlock ifEndBasicBlock = _genContext.Context.CreateBasicBlock("if.end");

            _builder.BuildCondBr(condVal, ifThenBasicBlock,
                ifElseBasicBlock.IsValid ? ifElseBasicBlock : ifEndBasicBlock);

            _builder.PositionAtEnd(ifThenBasicBlock);
            leaf.IfBody.Accept(this);
            BuildBrIfNecessary(ifEndBasicBlock);
            if (leaf.ElseBody != null) {
                _function.FunctionValue.AppendExistingBasicBlock(ifElseBasicBlock);
                _builder.PositionAtEnd(ifElseBasicBlock);
                leaf.ElseBody.Accept(this);
                BuildBrIfNecessary(ifEndBasicBlock);
            }

            _function.FunctionValue.AppendExistingBasicBlock(ifEndBasicBlock);
            _builder.PositionAtEnd(ifEndBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) leaf.Value, true);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;
            
            _visitedValue = _builder.BuildGlobalStringPtr(leaf.Value);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf.Condition, out Symbol condRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    condRange.LLVMLine, _genContext.ColumnInfo ? condRange.LLVMColumn : 0);
            }

            BasicBlock condBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.cond");
            BasicBlock bodyBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.body");
            BasicBlock endBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.end");

            SetCurrentDebugLocation(leaf);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(condBasicBlock);
            leaf.Condition.Accept(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            condVal = ConvertToBool(condVal);

            _builder.BuildCondBr(condVal, bodyBasicBlock, endBasicBlock);

            _builder.PositionAtEnd(bodyBasicBlock);
            BreakContinue oldBreakContinueTop = _breakContinueTop;
            _breakContinueTop = new BreakContinue(endBasicBlock, condBasicBlock);
            leaf.Body.Accept(this);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _builder.BuildBr(_breakContinueTop.BreakBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _builder.BuildBr(_breakContinueTop.ContinueBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            if (leaf.RHS == null) {
                SetCurrentDebugLocation(leaf);
                if (_function.ReturnBlock != null) {
                    _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
                } else {
                    _visitedValue = _builder.BuildRetVoid();
                }

                _builder.ClearInsertionPosition();
                return;
            }

            leaf.RHS.Accept(this);
            Value retVal = _visitedValue;
            if (!retVal.IsValid) {
                throw new InvalidOperationException("return did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            retVal = ConvertToType(retVal, _function.FunctionType.ReturnType);

            if (_function.ReturnBlock != null && _function.RetvalStorage != null) {
                _builder.BuildStore(retVal, _function.RetvalStorage.Value);
                _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
            } else {
                _visitedValue = _builder.BuildRet(retVal);
            }

            _builder.ClearInsertionPosition();
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            if (_builder == null) {
                throw new InvalidOperationException("NULL BUILDER");
            }

            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

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
            throw new InvalidOperationException("Enum leaf shouldn't be part of a function AST");
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            int value = leaf.Enum.Enumerations.First(kvp => kvp.Key == leaf.Name).Value;
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) value, true);
        }

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
            throw new InvalidOperationException("Type specifier leaf shouldn't be part of a function AST");
        }
    }
}