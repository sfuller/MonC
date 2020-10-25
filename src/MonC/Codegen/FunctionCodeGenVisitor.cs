using System;
using System.Collections.Generic;
using MonC.IL;
using MonC.Semantics;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class FunctionCodeGenVisitor : IStatementVisitor, IExpressionVisitor, IBasicExpressionVisitor
    {
        private readonly FunctionBuilder _functionBuilder;

        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;
        private readonly SemanticModule _module;
        private readonly SemanticContext _semanticContext;
        private readonly StructLayoutManager _structLayoutManager;
        private readonly ILTypeSizeManager _typeSizeManager;
        private readonly List<string> _strings;

        private readonly Stack<int> _breaks = new Stack<int>();
        private readonly Stack<int> _continues = new Stack<int>();

        public FunctionCodeGenVisitor(
            FunctionBuilder functionBuilder,
            FunctionStackLayout layout,
            FunctionManager functionManager,
            SemanticModule module,
            SemanticContext semanticContext,
            StructLayoutManager structLayoutManager,
            ILTypeSizeManager typeSizeManager,
            List<string> strings
        )
        {
            _functionBuilder = functionBuilder;
            _layout = layout;
            _functionManager = functionManager;
            _module = module;
            _semanticContext = semanticContext;
            _structLayoutManager = structLayoutManager;
            _typeSizeManager = typeSizeManager;
            _strings = strings;
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            node.RHS.AcceptExpressionVisitor(this);
            node.LHS.AcceptExpressionVisitor(this);

            int comparisonOperationAddress = _functionBuilder.InstructionCount;

            BinaryOperationCodeGenVisitor binOpVisitor = new BinaryOperationCodeGenVisitor(_functionBuilder);
            binOpVisitor.Setup();
            node.AcceptBinaryOperationVisitor(binOpVisitor);


            _functionBuilder.AddDebugSymbol(comparisonOperationAddress, node);
        }

        public void VisitBasicExpression(IBasicExpression node)
        {
            node.AcceptBasicExpressionVisitor(this);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            UnaryOperationCodeGenVisitor unaryVisitor = new UnaryOperationCodeGenVisitor(_functionBuilder, this);
            node.AcceptUnaryOperationVisitor(unaryVisitor);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            AssignmentCodeGenVisitor assignmentCodeGenVisitor
                    = new AssignmentCodeGenVisitor(_layout, _module, _structLayoutManager);
            node.Lhs.AcceptAssignableVisitor(assignmentCodeGenVisitor);

            node.Rhs.AcceptExpressionVisitor(this);

             int addr = _functionBuilder.AddInstruction(OpCode.WRITE, assignmentCodeGenVisitor.AssignmentWriteLocation);
             _functionBuilder.AddDebugSymbol(addr, node);
        }

        public void VisitAccess(AccessNode node)
        {
            node.Lhs.AcceptExpressionVisitor(this);

            StructType structType = (StructType) _module.ExpressionResultTypes[node.Lhs];
            StructLayout layout = _structLayoutManager.GetLayout(structType);
            if (!layout.MemberOffsets.TryGetValue(node.Rhs, out int offset)) {
                throw new InvalidOperationException();
            }

            int structSize = _typeSizeManager.GetSize(structType);
            int fieldSize = _typeSizeManager.GetSize(((TypeSpecifierNode) node.Rhs.Type).Type);

            int trim = structSize - offset - fieldSize;
            _functionBuilder.AddInstruction(OpCode.POP, 0, trim);
            _functionBuilder.AddInstruction(OpCode.ACCESS, offset, offset + fieldSize);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            int value = _semanticContext.EnumInfo[node.Declaration.Name].Value;
            _functionBuilder.AddInstruction(OpCode.PUSHWORD, value);
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            node.Assignment.AcceptExpressionVisitor(this);

            IType expressionType = _module.ExpressionResultTypes[node.Assignment];
            int resultSize = _typeSizeManager.GetSize(expressionType);

            int addr = _functionBuilder.AddInstruction(OpCode.WRITE, _layout.Variables[node], resultSize);
            _functionBuilder.AddInstruction(OpCode.POP, 0, resultSize);

            _functionBuilder.AddDebugSymbol(addr, node);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration.AcceptStatementVisitor(this);

            // jump straight to condition
            int initialJumpLocation = _functionBuilder.AddInstruction(OpCode.JUMP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(node.Body);

            int continueJumpLocation = _functionBuilder.InstructionCount;

            // Generate update code
            node.Update.AcceptExpressionVisitor(this);
            _functionBuilder.AddInstruction(OpCode.POP); // TODO: Use size of expression result.

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            node.Condition.AcceptExpressionVisitor(this);

            // Branch to body if condition met.
            int currentLocation = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(OpCode.JUMPNZ, bodyLocation);

            _functionBuilder.Instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation);

            int breakJumpLocation = _functionBuilder.InstructionCount;

            foreach (int breakLocation in _breaks) {
                _functionBuilder.Instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation);
            }
            _breaks.Clear();

            foreach (int continueLocation in _continues) {
                _functionBuilder.Instructions[continueLocation] = new Instruction(OpCode.JUMP, continueJumpLocation);
            }
            _continues.Clear();
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            int argumentsSize = 0;

            foreach (IExpressionNode argument in node.Arguments) {
                IType expressionType = _module.ExpressionResultTypes[argument];
                argumentsSize += _typeSizeManager.GetSize(expressionType);

                argument.AcceptExpressionVisitor(this);
            }

            int functionLoadAddr = _functionBuilder.AddInstruction(OpCode.PUSHWORD, _functionManager.GetFunctionIndex(node.LHS));
            _functionBuilder.SetInstructionReferencingFunctionAddress(functionLoadAddr);

            _functionBuilder.AddInstruction(OpCode.CALL);

            _functionBuilder.FreeStackSpace(argumentsSize + sizeof(int)); // Extra word is function index that was pushed.

            IType returnType = ((TypeSpecifierNode) node.LHS.ReturnType).Type;
            _functionBuilder.AllocStackSpace(_typeSizeManager.GetSize(returnType));

            // Add debug symbol at the first instruction that starts preparing the function to be called.
            _functionBuilder.AddDebugSymbol(functionLoadAddr, node);
        }

        public void VisitVariable(VariableNode node)
        {
            IType type = ((TypeSpecifierNode) node.Declaration.Type).Type;
            int size = _typeSizeManager.GetSize(type);
            _functionBuilder.AddInstruction(OpCode.READ, _layout.Variables[node.Declaration], size);
        }

        public void VisitIfElse(IfElseNode node)
        {
            int startAddress = _functionBuilder.InstructionCount;
            _functionBuilder.AddDebugSymbol(startAddress, node.Condition);

            node.Condition.AcceptExpressionVisitor(this);

            // Make space for the branch instruction we will instert. We will set values later.
            int branchIndex = _functionBuilder.AddInstruction(OpCode.JUMPZ);

            VisitBody(node.IfBody);
            // Jump to end of if/else after evaluation of if body.
            int ifEndIndex = _functionBuilder.AddInstruction(OpCode.JUMP);

            int bodyIndex = _functionBuilder.InstructionCount;
            VisitBody(node.ElseBody);

            int endIndex = _functionBuilder.InstructionCount;

            _functionBuilder.Instructions[branchIndex] = new Instruction(OpCode.JUMPZ, bodyIndex);
            _functionBuilder.Instructions[ifEndIndex] =  new Instruction(OpCode.JUMP, endIndex);
        }

        public void VisitVoid(VoidExpressionNode node)
        {
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            _functionBuilder.AddInstruction(OpCode.PUSHWORD, node.Value);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            int index = _strings.Count;
            _strings.Add(node.Value);
            int addr = _functionBuilder.AddInstruction(OpCode.PUSHWORD, index);
            _functionBuilder.SetStringInstruction(addr);
        }

        public void VisitWhile(WhileNode node)
        {
            // jump straight to condition
            int initialJumpLocation = _functionBuilder.AddInstruction(OpCode.JUMP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(node.Body);

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            node.Condition.AcceptExpressionVisitor(this);

            // Branch to body if condition met.
            int currentLocation = _functionBuilder.InstructionCount;
            _functionBuilder.AddInstruction(OpCode.JUMPNZ, bodyLocation);

            _functionBuilder.Instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation);

            int breakJumpLocation = _functionBuilder.InstructionCount;

            foreach (int breakLocation in _breaks) {
                _functionBuilder.Instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation);
            }
            _breaks.Clear();
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression.AcceptExpressionVisitor(this);
            // Remove Result From Stack
            IType expressionType = _module.ExpressionResultTypes[node.Expression];
            int resultSize = _typeSizeManager.GetSize(expressionType);
            _functionBuilder.AddInstruction(OpCode.POP, 0, resultSize);
        }

        public void VisitBreak(BreakNode node)
        {
            int breakIndex = _functionBuilder.AddInstruction(OpCode.JUMP);
            _breaks.Push(breakIndex);
            _functionBuilder.AddDebugSymbol(breakIndex, node);
        }

        public void VisitContinue(ContinueNode node)
        {
            int continueIndex = _functionBuilder.AddInstruction(OpCode.JUMP);
            _continues.Push(continueIndex);
            _functionBuilder.AddDebugSymbol(continueIndex, node);
        }

        public void VisitReturn(ReturnNode node)
        {
            node.RHS.AcceptExpressionVisitor(this);

            // TODO: Use actual size of return type.
            // Return value is always the beginning of the stack.
            _functionBuilder.AddInstruction(OpCode.WRITE, 0, sizeof(int));

            // TODO: Is this POP necesary? The VM doesn't care about extra values on the stack after RETURN.
            // Pop the value off of the top of the stack now that we've written it to the return value location.
            _functionBuilder.AddInstruction(OpCode.POP);

            int addr = _functionBuilder.AddInstruction(OpCode.RETURN);
            _functionBuilder.AddDebugSymbol(addr, node);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            throw new InvalidOperationException("Unexpected expression node type. Was replacement of a parse tree node missed?");
        }
    }
}
