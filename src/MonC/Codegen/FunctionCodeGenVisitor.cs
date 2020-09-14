using System;
using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;
using MonC.SyntaxTree.Leaves.Statements;

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

        private void AddDebugSymbol(int address, ISyntaxTreeLeaf associatedLeaf)
        {
            _functionBuilder.AddDebugSymbol(address, associatedLeaf);
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
            bool shouldCoerceSides = leaf is LogicalAndBinOpLeaf;

            // Evaluate right hand side and put it in the stack.
            int rhsStackAddress = AllocTemporaryStackAddress();
            leaf.RHS.AcceptExpressionVisitor(this);
            if (shouldCoerceSides) {
                AddInstruction(OpCode.BOOL);
            }
            AddInstruction(OpCode.WRITE, rhsStackAddress);

            // Evaluate left hand side and keep it in the a register
            leaf.LHS.AcceptExpressionVisitor(this);

            int comparisonOperationAddress = _functionBuilder.InstructionCount;


            BinaryOperationCodeGenVisitor binOpVisitor = new BinaryOperationCodeGenVisitor(_functionBuilder);
            binOpVisitor.Setup(rhsStackAddress);
            leaf.AcceptBinaryOperationVisitor(binOpVisitor);

            FreeTemporaryStackAddress();

            AddDebugSymbol(comparisonOperationAddress, leaf);
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
            UnaryOperationCodeGenVisitor unaryVisitor = new UnaryOperationCodeGenVisitor(_functionBuilder, this);
            leaf.AcceptUnaryOperationVisitor(unaryVisitor);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS.AcceptExpressionVisitor(this);
            int variableAddress;
            _layout.Variables.TryGetValue(leaf.Declaration, out variableAddress);
            int addr = AddInstruction(OpCode.WRITE, variableAddress);
            AddDebugSymbol(addr, leaf);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            int value = leaf.Enum.Enumerations.First(kvp => kvp.Key == leaf.Name).Value;
            AddInstruction(OpCode.LOAD, value);
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            // TODO: Support not doing an assignment when expression is void.
            // Maybe this could be accomplished by having an ExpressionCodeGenVisitor that return if a value was set.
            // If we transition the bytecode format to be more stack based, the ExpressionCodeGenVisitor could return
            // how many values were pushed.
            leaf.Assignment.AcceptExpressionVisitor(this);
            int addr = AddInstruction(OpCode.WRITE, _layout.Variables[leaf]);
            AddDebugSymbol(addr, leaf);
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.AcceptStatementVisitor(this);

            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(leaf.Body);

            int continueJumpLocation = _functionBuilder.InstructionCount;

            // Generate update code
            leaf.Update.AcceptExpressionVisitor(this);

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            leaf.Condition.AcceptExpressionVisitor(this);

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

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            int argumentStackLength = leaf.ArgumentCount + 1;
            int argumentStack = AllocTemporaryStackAddress(argumentStackLength);

            // First argument is the index of the function to be called
            int functionLoadAddr = AddInstruction(OpCode.LOAD, _functionManager.GetFunctionIndex(leaf.LHS));
            AddInstruction(OpCode.WRITE, argumentStack);

            _functionBuilder.SetInstructionReferencingFunctionAddress(functionLoadAddr);

            // The rest of the argument stack is the argument values.
            int argumentStackValuesStart = argumentStack + 1;

            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).AcceptExpressionVisitor(this);
                AddInstruction(OpCode.WRITE, argumentStackValuesStart + i);
            }

            AddInstruction(OpCode.CALL, argumentStack);

            FreeTemporaryStackAddress(argumentStackLength);

            // Add debug symbol at the first instruction that starts preparing the function to be called.
            AddDebugSymbol(functionLoadAddr, leaf);
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            AddInstruction(OpCode.READ, _layout.Variables[leaf.Declaration]);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            int startAddress = _functionBuilder.InstructionCount;
            AddDebugSymbol(startAddress, leaf.Condition);

            leaf.Condition.AcceptExpressionVisitor(this);

            // Make space for the branch instruction we will instert.
            int branchIndex = AddInstruction(OpCode.NOOP);

            VisitBody(leaf.IfBody);
            // Jump to end of if/else after evaluation of if body.
            int ifEndIndex = AddInstruction(OpCode.NOOP);

            VisitBody(leaf.ElseBody);

            int endIndex = _functionBuilder.InstructionCount;

            _functionBuilder.Instructions[branchIndex] = new Instruction(OpCode.JUMPZ, ifEndIndex - branchIndex);
            _functionBuilder.Instructions[ifEndIndex] =  new Instruction(OpCode.JUMP, endIndex - ifEndIndex - 1);
        }

        public void VisitVoid(VoidExpression leaf)
        {
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            AddInstruction(OpCode.LOAD, leaf.Value);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            int index = _strings.Count;
            _strings.Add(leaf.Value);
            int addr = AddInstruction(OpCode.LOAD, index);
            _functionBuilder.SetStringInstruction(addr);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _functionBuilder.InstructionCount;
            VisitBody(leaf.Body);

            // Generate condition code
            int conditionLocation = _functionBuilder.InstructionCount;
            leaf.Condition.AcceptExpressionVisitor(this);

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

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            leaf.Expression.AcceptExpressionVisitor(this);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            int breakIndex = AddInstruction(OpCode.NOOP);
            _breaks.Push(breakIndex);
            AddDebugSymbol(breakIndex, leaf);
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            int continueIndex = AddInstruction(OpCode.NOOP);
            _continues.Push(continueIndex);
            AddDebugSymbol(continueIndex, leaf);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            leaf.RHS.AcceptExpressionVisitor(this);
            int addr = AddInstruction(OpCode.RETURN);
            AddDebugSymbol(addr, leaf);
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
            throw new InvalidOperationException("Unexpected expression leaf type. Was replacement of a parse tree leaf missed?");
        }

        private void VisitBody(Body leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IStatementLeaf statement = leaf.GetStatement(i);
                statement.AcceptStatementVisitor(this);
            }
        }
    }
}
