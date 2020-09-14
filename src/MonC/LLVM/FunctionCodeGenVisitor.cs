using System;
using System.Linq;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.LLVM
{
    public class FunctionCodeGenVisitor : IStatementVisitor, IExpressionVisitor
    {
        internal CodeGeneratorContext _genContext;
        internal CodeGeneratorContext.Function _function;
        internal Builder _builder;
        internal BasicBlock _basicBlock;
        internal Metadata _lexicalScope;
        internal Value _visitedValue;

        internal FunctionCodeGenVisitor(CodeGeneratorContext genContext, CodeGeneratorContext.Function function,
            Builder builder, BasicBlock basicBlock)
        {
            _genContext = genContext;
            _function = function;
            _builder = builder;
            _basicBlock = basicBlock;
            _lexicalScope = _function.DiFunctionDef;
            _visitedValue = new Value();
            _breakContinueTop = new BreakContinue();
        }

        internal Metadata SetCurrentDebugLocation(ISyntaxTreeLeaf leaf)
        {
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
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            _builder.BuildBr(dest);
            _builder.ClearInsertionPosition();
        }

        internal Value ConvertToBool(Value val, bool invert = false)
        {
            // TODO: Support pointers
            Type valType = val.TypeOf;
            if (valType.Kind == CAPI.LLVMTypeKind.Integer) {
                if (valType.IntTypeWidth == 1) {
                    return val;
                }

                return _builder.BuildICmp(invert ? CAPI.LLVMIntPredicate.IntEQ : CAPI.LLVMIntPredicate.IntNE, val,
                    Value.ConstInt(valType, 0, true), "tobool");
            } else if (valType.IsFloatingPointType()) {
                return _builder.BuildFCmp(invert ? CAPI.LLVMRealPredicate.RealUEQ : CAPI.LLVMRealPredicate.RealUNE, val,
                    Value.ConstReal(valType, 0.0), "tobool");
            }

            throw new InvalidOperationException("only integers and floating point values can be converted to bool");
        }

        internal CAPI.LLVMOpcode GetCastOpcode(Value val, Type tp)
        {
            Type valType = val.TypeOf;

            // TODO: This is a hack to not sign-extend bools; Make a better way to track signedness
            bool sext = valType.Kind == CAPI.LLVMTypeKind.Integer && valType.IntTypeWidth != 1;

            // TODO: support unsigned values
            return Builder.GetCastOpcode(val, sext, tp, true);
        }

        private Value ConvertToType(Value val, Type tp)
        {
            if (val.TypeOf == tp) {
                return val;
            }

            CAPI.LLVMOpcode castOp = GetCastOpcode(val, tp);
            return _builder.BuildCast(castOp, val, tp);
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf) =>
            leaf.AcceptBinaryOperationVisitor(new BinaryOperationCodeGenVisitor(this));

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            leaf.RHS.AcceptExpressionVisitor(this);
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

        public void VisitBody(Body leaf)
        {
            Metadata oldLexicalScope = _lexicalScope;

            // Push new lexical block
            if (_genContext.DiBuilder != null) {
                // TODO: BodyNode needs to be implemented
                //_genContext.TryGetTokenSymbol(leaf, out Symbol range);
                //_lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                //    range.LLVMLine, _genContext.ColumnInfo ? range.LLVMColumn : 0);
                _lexicalScope = _function.DiFunctionDef;
            }

            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IStatementLeaf statement = leaf.GetStatement(i);
                statement.AcceptStatementVisitor(this);
            }

            _lexicalScope = oldLexicalScope;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            leaf.Assignment.AcceptExpressionVisitor(this);
            if (!_visitedValue.IsValid) {
                return;
            }

            Metadata dbgLocation = SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf, out Value varStorage);

            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf, out Symbol varRange);
                Metadata varType = _genContext.LookupDiType(leaf.Type);
                Metadata varMetadata = _genContext.DiBuilder.CreateAutoVariable(_lexicalScope, leaf.Name,
                    _genContext.DiFile, varRange.LLVMLine, varType, true, CAPI.LLVMDIFlags.Zero,
                    varType.GetTypeAlignInBits());
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
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf.Declaration, out Symbol declRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    declRange.LLVMLine, _genContext.ColumnInfo ? declRange.LLVMColumn : 0);
            }

            leaf.Declaration.AcceptStatementVisitor(this);

            BasicBlock condBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.cond");
            BasicBlock bodyBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.body");
            BasicBlock incBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.inc");
            BasicBlock endBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.end");

            SetCurrentDebugLocation(leaf);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(condBasicBlock);
            leaf.Condition.AcceptExpressionVisitor(this);
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
            VisitBody(leaf.Body);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(incBasicBlock);

            _builder.PositionAtEnd(incBasicBlock);
            leaf.Update.AcceptExpressionVisitor(this);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            CodeGeneratorContext.Function func = _genContext.GetFunctionDeclaration(leaf.LHS);
            Type[] paramTypes = func.FunctionType.ParamTypes;

            Value[] args = new Value[leaf.ArgumentCount];
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).AcceptExpressionVisitor(this);
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
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildLoad(_genContext.LookupType(leaf.Declaration.Type), varStorage);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DiBuilder != null) {
                _genContext.TryGetTokenSymbol(leaf.Condition, out Symbol condRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    condRange.LLVMLine, _genContext.ColumnInfo ? condRange.LLVMColumn : 0);
            }

            leaf.Condition.AcceptExpressionVisitor(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            condVal = ConvertToBool(condVal);

            BasicBlock ifThenBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "if.then");
            BasicBlock ifElseBasicBlock = BasicBlock.Null;
            if (leaf.ElseBody.Length > 0)
                ifElseBasicBlock = _genContext.Context.CreateBasicBlock("if.else");
            BasicBlock ifEndBasicBlock = _genContext.Context.CreateBasicBlock("if.end");

            _builder.BuildCondBr(condVal, ifThenBasicBlock,
                ifElseBasicBlock.IsValid ? ifElseBasicBlock : ifEndBasicBlock);

            _builder.PositionAtEnd(ifThenBasicBlock);
            VisitBody(leaf.IfBody);
            BuildBrIfNecessary(ifEndBasicBlock);
            if (leaf.ElseBody.Length > 0) {
                _function.FunctionValue.AppendExistingBasicBlock(ifElseBasicBlock);
                _builder.PositionAtEnd(ifElseBasicBlock);
                VisitBody(leaf.ElseBody);
                BuildBrIfNecessary(ifEndBasicBlock);
            }

            _function.FunctionValue.AppendExistingBasicBlock(ifEndBasicBlock);
            _builder.PositionAtEnd(ifEndBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitVoid(VoidExpression leaf)
        {
            _visitedValue = new Value();
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) leaf.Value, true);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            _visitedValue = _builder.BuildGlobalStringPtr(leaf.Value);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
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
            leaf.Condition.AcceptExpressionVisitor(this);
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
            VisitBody(leaf.Body);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            leaf.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _builder.BuildBr(_breakContinueTop.BreakBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(leaf);
            _builder.BuildBr(_breakContinueTop.ContinueBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            if (leaf.RHS is VoidExpression) {
                SetCurrentDebugLocation(leaf);
                if (_function.ReturnBlock != null) {
                    _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
                } else {
                    _visitedValue = _builder.BuildRetVoid();
                }

                _builder.ClearInsertionPosition();
                return;
            }

            leaf.RHS.AcceptExpressionVisitor(this);
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
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            leaf.RHS.AcceptExpressionVisitor(this);
            if (!_visitedValue.IsValid) {
                throw new InvalidOperationException("assignment did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(leaf);
            _function.VariableValues.TryGetValue(leaf.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildStore(_visitedValue, varStorage);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            int value = leaf.Enum.Enumerations.First(kvp => kvp.Key == leaf.Name).Value;
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) value, true);
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
            throw new InvalidOperationException(
                "Unexpected expression leaf type. Was replacement of a parse tree leaf missed?");
        }
    }
}
