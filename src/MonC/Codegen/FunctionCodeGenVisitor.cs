using System;
using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class FunctionCodeGenVisitor : IStatementVisitor, IExpressionVisitor
    {
        private readonly FunctionBuilder _functionBuilder;

        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;
        private readonly List<string> _strings;

        private readonly Stack<int> _breaks = new Stack<int>();
        private readonly Stack<int> _continues = new Stack<int>();

        public FunctionCodeGenVisitor(
            FunctionBuilder functionBuilder,
            FunctionStackLayout layout,
            FunctionManager functionManager,
            List<string> strings
        )
        {
            _layout = layout;
            _functionManager = functionManager;
            _strings = strings;
            _functionBuilder = functionBuilder;
        }

        public int AllocTemporaryStackAddress(int length = 1)
        {
            return _functionBuilder.AllocTemporaryStackAddress(length);
        }

        public int FreeTemporaryStackAddress(int length = 1)
        {
            return _functionBuilder.FreeTemporaryStackAddress(length);
        }

        private int AddInstruction(OpCode op, int immediate = 0)
        {
            return _functionBuilder.AddInstruction(op, immediate);
        }

        private void AddDebugSymbol(int address, ISyntaxTreeNode associatedNode)
        {
            _functionBuilder.AddDebugSymbol(address, associatedNode);
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
            bool shouldCoerceSides = node is LogicalAndBinOpNode;

            // Evaluate right hand side and put it in the stack.
            int rhsStackAddress = AllocTemporaryStackAddress();
            node.RHS.AcceptExpressionVisitor(this);
            if (shouldCoerceSides) {
                AddInstruction(OpCode.BOOL);
            }
            AddInstruction(OpCode.WRITE, rhsStackAddress);

            // Evaluate left hand side and keep it in the a register
            node.LHS.AcceptExpressionVisitor(this);

            int comparisonOperationAddress = _functionBuilder.InstructionCount;


            BinaryOperationCodeGenVisitor binOpVisitor = new BinaryOperationCodeGenVisitor(_functionBuilder);
            binOpVisitor.Setup(rhsStackAddress);
            node.AcceptBinaryOperationVisitor(binOpVisitor);

            FreeTemporaryStackAddress();

            AddDebugSymbol(comparisonOperationAddress, node);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            UnaryOperationCodeGenVisitor unaryVisitor = new UnaryOperationCodeGenVisitor(_functionBuilder, this);
            node.AcceptUnaryOperationVisitor(unaryVisitor);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            node.RHS.AcceptExpressionVisitor(this);
            int variableAddress;
            _layout.Variables.TryGetValue(node.Declaration, out variableAddress);
            int addr = AddInstruction(OpCode.WRITE, variableAddress);
            AddDebugSymbol(addr, node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            int value = node.Enum.Enumerations.First(kvp => kvp.Key == node.Name).Value;
            AddInstruction(OpCode.LOAD, value);
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
            int addr = AddInstruction(OpCode.WRITE, _layout.Variables[node]);
            AddDebugSymbol(addr, node);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration.AcceptStatementVisitor(this);

            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(node.Body);

            int continueJumpLocation = _functionBuilder.InstructionCount;

            // Generate update code
            node.Update.AcceptExpressionVisitor(this);

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            node.Condition.AcceptExpressionVisitor(this);

            // Branch to body if condition met.
            int currentLocation = _functionBuilder.InstructionCount;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation - 1);

            _functionBuilder.Instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation - 1);

            int breakJumpLocation = _functionBuilder.InstructionCount;

            foreach (int breakLocation in _breaks) {
                _functionBuilder.Instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation - breakLocation - 1);
            }
            _breaks.Clear();

            foreach (int continueLocation in _continues) {
                _functionBuilder.Instructions[continueLocation] = new Instruction(OpCode.JUMP, continueJumpLocation - continueLocation - 1);
            }
            _continues.Clear();
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            int argumentStackLength = node.ArgumentCount + 1;
            int argumentStack = AllocTemporaryStackAddress(argumentStackLength);

            // First argument is the index of the function to be called
            int functionLoadAddr = AddInstruction(OpCode.LOAD, _functionManager.GetFunctionIndex(node.LHS));
            AddInstruction(OpCode.WRITE, argumentStack);

            _functionBuilder.SetInstructionReferencingFunctionAddress(functionLoadAddr);

            // The rest of the argument stack is the argument values.
            int argumentStackValuesStart = argumentStack + 1;

            for (int i = 0, ilen = node.ArgumentCount; i < ilen; ++i) {
                node.GetArgument(i).AcceptExpressionVisitor(this);
                AddInstruction(OpCode.WRITE, argumentStackValuesStart + i);
            }

            AddInstruction(OpCode.CALL, argumentStack);

            FreeTemporaryStackAddress(argumentStackLength);

            // Add debug symbol at the first instruction that starts preparing the function to be called.
            AddDebugSymbol(functionLoadAddr, node);
        }

        public void VisitVariable(VariableNode node)
        {
            AddInstruction(OpCode.READ, _layout.Variables[node.Declaration]);
        }

        public void VisitIfElse(IfElseNode node)
        {
            int startAddress = _functionBuilder.InstructionCount;
            AddDebugSymbol(startAddress, node.Condition);

            node.Condition.AcceptExpressionVisitor(this);

            // Make space for the branch instruction we will instert.
            int branchIndex = AddInstruction(OpCode.NOOP);

            VisitBody(node.IfBody);
            // Jump to end of if/else after evaluation of if body.
            int ifEndIndex = AddInstruction(OpCode.NOOP);

            VisitBody(node.ElseBody);

            int endIndex = _functionBuilder.InstructionCount;

            _functionBuilder.Instructions[branchIndex] = new Instruction(OpCode.JUMPZ, ifEndIndex - branchIndex);
            _functionBuilder.Instructions[ifEndIndex] =  new Instruction(OpCode.JUMP, endIndex - ifEndIndex - 1);
        }

        public void VisitVoid(VoidExpressionNode node)
        {
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            AddInstruction(OpCode.LOAD, node.Value);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            int index = _strings.Count;
            _strings.Add(node.Value);
            int addr = AddInstruction(OpCode.LOAD, index);
            _functionBuilder.SetStringInstruction(addr);
        }

        public void VisitWhile(WhileNode node)
        {
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(node.Body);

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            node.Condition.AcceptExpressionVisitor(this);

            // Branch to body if condition met.
            int currentLocation = _functionBuilder.InstructionCount;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation - 1);

            _functionBuilder.Instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation - 1);

            int breakJumpLocation = _functionBuilder.InstructionCount;

            foreach (int breakLocation in _breaks) {
                _functionBuilder.Instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation - breakLocation - 1);
            }
            _breaks.Clear();
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitBreak(BreakNode node)
        {
            int breakIndex = AddInstruction(OpCode.NOOP);
            _breaks.Push(breakIndex);
            AddDebugSymbol(breakIndex, node);
        }

        public void VisitContinue(ContinueNode node)
        {
            int continueIndex = AddInstruction(OpCode.NOOP);
            _continues.Push(continueIndex);
            AddDebugSymbol(continueIndex, node);
        }

        public void VisitReturn(ReturnNode node)
        {
            node.RHS.AcceptExpressionVisitor(this);
            int addr = AddInstruction(OpCode.RETURN);
            AddDebugSymbol(addr, node);
        }

        public void VisitUnknown(IExpressionNode node)
        {
            throw new InvalidOperationException("Unexpected expression node type. Was replacement of a parse tree node missed?");
        }
    }
}
