using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;
        private readonly IDictionary<int, TokenRange> _addressToTokenMap;
        private readonly IDictionary<IASTLeaf, TokenRange> _leafToTokenMap;
        
        private readonly List<Instruction> _instructions;

        private readonly Stack<int> _breaks = new Stack<int>();
        
        public CodeGenVisitor(
            FunctionStackLayout layout,
            List<Instruction> instructions,
            FunctionManager functionManager,
            IDictionary<int, TokenRange> addressToTokenMap,
            IDictionary<IASTLeaf, TokenRange> leafToTokenMap
        )
        {
            _layout = layout;
            _instructions = instructions;
            _functionManager = functionManager;
            _addressToTokenMap = addressToTokenMap;
            _leafToTokenMap = leafToTokenMap;
        }

        private int AddInstruction(OpCode op, int immediate = 0)
        {
            int index = _instructions.Count;
            _instructions.Add(new Instruction(op, immediate));
            return index;
        }

        private void AddDebugSymbol(int address, IASTLeaf associatedLeaf)
        {
            TokenRange range;
            _leafToTokenMap.TryGetValue(associatedLeaf, out range);
            _addressToTokenMap[address] = range;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            // Evaluate right hand side and put it in the b register
            leaf.RHS.Accept(this);
            AddInstruction(OpCode.LOADB);
            
            // Evaluate left hand side and put it in the a register
            leaf.LHS.Accept(this);

            int comparisonOperationAddress = _instructions.Count;
            
            switch (leaf.Op.Value) {
                case ">":
                case "<":
                case Syntax.GREATER_THAN_OR_EQUAL_TO:
                case Syntax.LESS_THAN_OR_EQUAL_TO:
                    GenerateRelationalComparison(leaf.Op);
                    break;
                case Syntax.EQUALS:
                    AddInstruction(OpCode.CMPE);
                    break;
                case Syntax.NOT_EQUALS:
                    AddInstruction(OpCode.CMPE);
                    AddInstruction(OpCode.NOT);
                    AddInstruction(OpCode.BOOL);
                    break;
                case Syntax.LOGICAL_OR:
                    AddInstruction(OpCode.OR);
                    AddInstruction(OpCode.BOOL);
                    break;
                case Syntax.LOGICAL_AND:
                    AddInstruction(OpCode.AND);
                    AddInstruction(OpCode.BOOL);
                    break;
                case "+":
                    AddInstruction(OpCode.ADD);
                    break;
                case "-":
                    AddInstruction(OpCode.SUB);
                    break;
                case "*":
                    AddInstruction(OpCode.MUL);
                    break;
                case "/":
                    AddInstruction(OpCode.DIV);
                    break;
                case "%":
                    AddInstruction(OpCode.MUL);
                    break;
                default:
                    throw new NotImplementedException(leaf.Op.Value);
            }
            
            AddDebugSymbol(comparisonOperationAddress, leaf);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            switch (leaf.Operator.Value) {
                case "-":
                    leaf.RHS.Accept(this);
                    int addr = AddInstruction(OpCode.LOADB);
                    AddInstruction(OpCode.LOAD, 0);
                    AddInstruction(OpCode.SUB);
                    AddDebugSymbol(addr, leaf);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void GenerateRelationalComparison(Token token)
        {
            if (token.Value.Contains("=")) {
                AddInstruction(OpCode.CMPLTE);
            } else {
                AddInstruction(OpCode.CMPLT);
            }

            if (token.Value.Contains(">")) {
                AddInstruction(OpCode.NOT);
                AddInstruction(OpCode.BOOL);
            }
        }
        
        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS.Accept(this);
            int addr = AddInstruction(OpCode.WRITE, _layout.Variables[leaf.Declaration]);
            AddDebugSymbol(addr, leaf);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            throw new NotImplementedException();
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            int value = Array.IndexOf(leaf.Enum.Enumerations, leaf.Name);
            AddInstruction(OpCode.LOAD, value);
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
            IASTLeaf assignment;
            if (leaf.Assignment.Get(out assignment)) {
                assignment.Accept(this);
                int addr = AddInstruction(OpCode.WRITE, _layout.Variables[leaf]);
                AddDebugSymbol(addr, leaf);
            }
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.Accept(this);
            
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _instructions.Count;
            leaf.Body.Accept(this);
            
            // Generate update code
            leaf.Update.Accept(this);

            // Generate condition code
            int conditionLocation = _instructions.Count;
            leaf.Condition.Accept(this);
            
            // Branch to body if condition met.
            int currentLocation = _instructions.Count;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
                AddInstruction(OpCode.PUSHARG);
            }
            
            int addr = AddInstruction(OpCode.CALL, _functionManager.GetFunctionIndex(leaf.LHS));
            AddDebugSymbol(addr, leaf);
        }
        
        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            leaf.Body.Accept(this);
            
            // All functions must end with a RETURN instruction.
            if (_instructions.Count == 0 || _instructions[_instructions.Count - 1].Op != OpCode.RETURN) {
                AddInstruction(OpCode.RETURN);
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            AddInstruction(OpCode.READ, _layout.Variables[leaf.Declaration]);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition.Accept(this);
            
            // Make space for the branch instruction we will instert.
            int branchIndex = AddInstruction(OpCode.NOOP);
            
            leaf.IfBody.Accept(this);
            // Jump to end of if/else after evaluation of if body.
            int ifEndIndex = AddInstruction(OpCode.NOOP);

            BodyLeaf elseBody;
            if (leaf.ElseBody.Get(out elseBody)) {
                elseBody.Accept(this);
            }

            int endIndex = _instructions.Count;

            _instructions[branchIndex] = new Instruction(OpCode.JUMPZ, ifEndIndex - branchIndex);
            _instructions[ifEndIndex] =  new Instruction(OpCode.JUMP, endIndex - ifEndIndex - 1);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            AddInstruction(OpCode.LOAD, leaf.Value);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            throw new NotImplementedException();
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            // jump straight to condition
            int initialJumpLocation = AddInstruction(OpCode.NOOP);

            // Generate body code
            int bodyLocation = _instructions.Count;
            leaf.Body.Accept(this);

            // Generate condition code
            int conditionLocation = _instructions.Count;
            leaf.Condition.Accept(this);
            
            // Branch to body if condition met.
            int currentLocation = _instructions.Count;
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            int breakIndex = AddInstruction(OpCode.NOOP);
            _breaks.Push(breakIndex);
            AddDebugSymbol(breakIndex, leaf);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            IASTLeaf rhs;
            if (leaf.RHS.Get(out rhs)) {
                rhs.Accept(this);
            }
            int addr = AddInstruction(OpCode.RETURN);
            AddDebugSymbol(addr, leaf);
        }
    }
}