using System;
using System.Linq;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.LLVM
{
    public class FunctionCodeGenVisitor : IStatementVisitor, IExpressionVisitor
    {
        internal readonly CodeGeneratorContext _genContext;
        internal readonly CodeGeneratorContext.Function _function;
        internal readonly Builder _builder;
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
        }

        internal Metadata SetCurrentDebugLocation(ISyntaxTreeNode node)
        {
            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node, out Symbol range);
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

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            node.AcceptBinaryOperationVisitor(new BinaryOperationCodeGenVisitor(this));
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            node.AcceptUnaryOperationVisitor(new UnaryOperationCodeGenVisitor(this));
        }

        public void VisitBody(BodyNode node)
        {
            Metadata oldLexicalScope = _lexicalScope;

            // Push new lexical block
            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node, out Symbol range);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    range.LLVMLine, _genContext.ColumnInfo ? range.LLVMColumn : 0);
            }

            node.VisitStatements(this);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            node.Assignment.AcceptExpressionVisitor(this);
            if (!_visitedValue.IsValid) {
                return;
            }

            Metadata dbgLocation = SetCurrentDebugLocation(node);
            _function.VariableValues.TryGetValue(node, out Value varStorage);

            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node, out Symbol varRange);
                Metadata varType = _genContext.LookupDiType(node.Type);
                Metadata varMetadata = _genContext.DiBuilder.CreateAutoVariable(_lexicalScope, node.Name,
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

        public void VisitFor(ForNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node.Declaration, out Symbol declRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    declRange.LLVMLine, _genContext.ColumnInfo ? declRange.LLVMColumn : 0);
            }

            node.Declaration.AcceptStatementVisitor(this);

            BasicBlock condBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.cond");
            BasicBlock bodyBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.body");
            BasicBlock incBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.inc");
            BasicBlock endBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "for.end");

            SetCurrentDebugLocation(node);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(condBasicBlock);
            node.Condition.AcceptExpressionVisitor(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(node);
            condVal = ConvertToBool(condVal);

            _builder.BuildCondBr(condVal, bodyBasicBlock, endBasicBlock);

            _builder.PositionAtEnd(bodyBasicBlock);
            BreakContinue oldBreakContinueTop = _breakContinueTop;
            _breakContinueTop = new BreakContinue(endBasicBlock, incBasicBlock);
            VisitBody(node.Body);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(incBasicBlock);

            _builder.PositionAtEnd(incBasicBlock);
            node.Update.AcceptExpressionVisitor(this);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            CodeGeneratorContext.Function func = _genContext.GetFunctionDeclaration(node.LHS);
            Type[] paramTypes = func.FunctionType.ParamTypes;

            Value[] args = new Value[node.ArgumentCount];
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                node.GetArgument(i).AcceptExpressionVisitor(this);
                if (!_visitedValue.IsValid) {
                    throw new InvalidOperationException("argument did not produce a usable rvalue");
                }

                args[i] = ConvertToType(_visitedValue, paramTypes[i]);
            }

            SetCurrentDebugLocation(node);
            _visitedValue = _builder.BuildCall(func.FunctionType, func.FunctionValue, args);
        }

        public void VisitVariable(VariableNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(node);
            _function.VariableValues.TryGetValue(node.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildLoad(_genContext.LookupType(node.Declaration.Type), varStorage);
        }

        public void VisitIfElse(IfElseNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node.Condition, out Symbol condRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    condRange.LLVMLine, _genContext.ColumnInfo ? condRange.LLVMColumn : 0);
            }

            node.Condition.AcceptExpressionVisitor(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(node);
            condVal = ConvertToBool(condVal);

            BasicBlock ifThenBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "if.then");
            BasicBlock ifElseBasicBlock = BasicBlock.Null;
            if (node.ElseBody.Statements.Count > 0)
                ifElseBasicBlock = _genContext.Context.CreateBasicBlock("if.else");
            BasicBlock ifEndBasicBlock = _genContext.Context.CreateBasicBlock("if.end");

            _builder.BuildCondBr(condVal, ifThenBasicBlock,
                ifElseBasicBlock.IsValid ? ifElseBasicBlock : ifEndBasicBlock);

            _builder.PositionAtEnd(ifThenBasicBlock);
            VisitBody(node.IfBody);
            BuildBrIfNecessary(ifEndBasicBlock);
            if (node.ElseBody.Statements.Count > 0) {
                _function.FunctionValue.AppendExistingBasicBlock(ifElseBasicBlock);
                _builder.PositionAtEnd(ifElseBasicBlock);
                VisitBody(node.ElseBody);
                BuildBrIfNecessary(ifEndBasicBlock);
            }

            _function.FunctionValue.AppendExistingBasicBlock(ifEndBasicBlock);
            _builder.PositionAtEnd(ifEndBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            _visitedValue = new Value();
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) node.Value, true);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            _visitedValue = _builder.BuildGlobalStringPtr(node.Value);
        }

        public void VisitWhile(WhileNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            Metadata oldLexicalScope = _lexicalScope;
            if (_genContext.DebugInfo) {
                _genContext.TryGetTokenSymbol(node.Condition, out Symbol condRange);
                _lexicalScope = _genContext.DiBuilder.CreateLexicalBlock(_lexicalScope, _genContext.DiFile,
                    condRange.LLVMLine, _genContext.ColumnInfo ? condRange.LLVMColumn : 0);
            }

            BasicBlock condBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.cond");
            BasicBlock bodyBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.body");
            BasicBlock endBasicBlock = _genContext.Context.AppendBasicBlock(_function.FunctionValue, "while.end");

            SetCurrentDebugLocation(node);
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(condBasicBlock);
            node.Condition.AcceptExpressionVisitor(this);
            Value condVal = _visitedValue;
            if (!condVal.IsValid) {
                throw new InvalidOperationException("condition did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(node);
            condVal = ConvertToBool(condVal);

            _builder.BuildCondBr(condVal, bodyBasicBlock, endBasicBlock);

            _builder.PositionAtEnd(bodyBasicBlock);
            BreakContinue oldBreakContinueTop = _breakContinueTop;
            _breakContinueTop = new BreakContinue(endBasicBlock, condBasicBlock);
            VisitBody(node.Body);
            _breakContinueTop = oldBreakContinueTop;
            BuildBrIfNecessary(condBasicBlock);

            _builder.PositionAtEnd(endBasicBlock);

            _lexicalScope = oldLexicalScope;
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitBreak(BreakNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(node);
            _builder.BuildBr(_breakContinueTop.BreakBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitContinue(ContinueNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            SetCurrentDebugLocation(node);
            _builder.BuildBr(_breakContinueTop.ContinueBlock);
            _builder.ClearInsertionPosition();
        }

        public void VisitReturn(ReturnNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            if (node.RHS is VoidExpressionNode) {
                SetCurrentDebugLocation(node);
                if (_function.ReturnBlock != null) {
                    _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
                } else {
                    _visitedValue = _builder.BuildRetVoid();
                }

                _builder.ClearInsertionPosition();
                return;
            }

            node.RHS.AcceptExpressionVisitor(this);
            Value retVal = _visitedValue;
            if (!retVal.IsValid) {
                throw new InvalidOperationException("return did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(node);
            retVal = ConvertToType(retVal, _function.FunctionType.ReturnType);

            if (_function.ReturnBlock != null && _function.RetvalStorage != null) {
                _builder.BuildStore(retVal, _function.RetvalStorage.Value);
                _visitedValue = _builder.BuildBr(_function.ReturnBlock.Value);
            } else {
                _visitedValue = _builder.BuildRet(retVal);
            }

            _builder.ClearInsertionPosition();
        }

        public void VisitAssignment(AssignmentNode node)
        {
            // Don't insert unreachable code
            if (!_builder.InsertBlock.IsValid)
                return;

            node.RHS.AcceptExpressionVisitor(this);
            if (!_visitedValue.IsValid) {
                throw new InvalidOperationException("assignment did not produce a usable rvalue");
            }

            SetCurrentDebugLocation(node);
            _function.VariableValues.TryGetValue(node.Declaration, out Value varStorage);
            _visitedValue = _builder.BuildStore(_visitedValue, varStorage);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            int value = node.Enum.Enumerations.First(kvp => kvp.Key == node.Name).Value;
            _visitedValue = Value.ConstInt(_genContext.Context.Int32Type, (ulong) value, true);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            throw new InvalidOperationException(
                "Unexpected expression node type. Was replacement of a parse tree node missed?");
        }
    }
}
