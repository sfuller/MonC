using System;
using System.Collections.Generic;
using MonC.IL;
using MonC.Semantics;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class FunctionCodeGenVisitor : IStatementVisitor, IExpressionVisitor, IBasicExpressionVisitor
    {
        private readonly FunctionBuilder _functionBuilder;

        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;
        private readonly List<string> _strings;
        private readonly Dictionary<string, EnumDeclarationInfo> _enumDeclarationInfos;

        private readonly Stack<int> _breaks = new Stack<int>();
        private readonly Stack<int> _continues = new Stack<int>();

        public FunctionCodeGenVisitor(
            FunctionBuilder functionBuilder,
            FunctionStackLayout layout,
            FunctionManager functionManager,
            List<string> strings,
            Dictionary<string, EnumDeclarationInfo> enumDeclarationInfos
        )
        {
            _layout = layout;
            _functionManager = functionManager;
            _strings = strings;
            _functionBuilder = functionBuilder;
            _enumDeclarationInfos = enumDeclarationInfos;
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
                    = new AssignmentCodeGenVisitor(node, _functionBuilder, _layout, this);
            node.Lhs.AcceptAssignableVisitor(assignmentCodeGenVisitor);
        }

        public void VisitAccess(AccessNode node)
        {
            // TODO: Put value of struct member into accumulator.
            throw new NotImplementedException();
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            int value = _enumDeclarationInfos[node.Declaration.Name].Value;
            _functionBuilder.AddInstruction(OpCode.PUSHWORD, value);
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            // TODO: Support not doing an assignment when expression is void.
            // Maybe this could be accomplished by having an ExpressionCodeGenVisitor that return if a value was set.
            // If we transition the bytecode format to be more stack based, the ExpressionCodeGenVisitor could return
            // how many values were pushed.
            node.Assignment.AcceptExpressionVisitor(this);

            // TODO: USE SIZE OF EXPRESSION TYPE FOR WRITE AND POP INSTRUCTION

            int addr = _functionBuilder.AddInstruction(OpCode.WRITE, _layout.Variables[node]);
            _functionBuilder.AddInstruction(OpCode.POP);

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
            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                node.GetArgument(i).AcceptExpressionVisitor(this);
            }

            int functionLoadAddr = _functionBuilder.AddInstruction(OpCode.PUSHWORD, _functionManager.GetFunctionIndex(node.LHS));
            _functionBuilder.SetInstructionReferencingFunctionAddress(functionLoadAddr);

            _functionBuilder.AddInstruction(OpCode.CALL);

            // TODO: GET ACTUAL SIZE OF ARGUMENTS
            _functionBuilder.FreeStackSpace(sizeof(int) * node.ArgumentCount + sizeof(int)); // Extra word is function index that was pushed.

            // TODO: GET ACTUAL SIZE OF RETURN VALUE
            _functionBuilder.AllocStackSpace(sizeof(int));

            // Add debug symbol at the first instruction that starts preparing the function to be called.
            _functionBuilder.AddDebugSymbol(functionLoadAddr, node);
        }

        public void VisitVariable(VariableNode node)
        {
            _functionBuilder.AddInstruction(OpCode.READ, _layout.Variables[node.Declaration]);
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
            // TODO: Determine size from expression result type
            _functionBuilder.AddInstruction(OpCode.POP);
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

            // TODO: Is this POP necesary?
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
