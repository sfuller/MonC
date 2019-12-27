using System;
using System.Collections.Generic;
using System.Linq;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
        private readonly FunctionStackLayout _layout;
        private readonly FunctionManager _functionManager;
        private readonly IDictionary<IASTLeaf, Symbol> _leafToTokenMap;
        
        private readonly IDictionary<int, Symbol> _addressToTokenMap = new Dictionary<int, Symbol>();
        private readonly List<Instruction> _instructions = new List<Instruction>();
        private readonly List<string> _strings;
        private readonly List<int> _stringInstructions = new List<int>();
        private readonly List<int> _instructionsReferencingFunctionAddresses = new List<int>();

        private readonly Stack<int> _breaks = new Stack<int>();

        
        /// <summary>
        /// The current end of the stack working memory space. This address will be used as the start of the next
        /// working memory allocation.
        /// </summary>
        private int _stackWorkOffset;
        
        /// <summary>
        /// This is the largest end range of this functions working memory at any time. Non-inclusive (Everythnig below
        /// this address is used in the stack, but nothing at this address or above it). 
        /// </summary>
        private int _maxStackWorkOffset;

        private int _argumentCount;
        
        public CodeGenVisitor(
            FunctionStackLayout layout,
            FunctionManager functionManager,
            IDictionary<IASTLeaf, Symbol> leafToTokenMap,
            List<string> strings
        )
        {
            _layout = layout;
            _functionManager = functionManager;
            _leafToTokenMap = leafToTokenMap;
            _strings = strings;

            if (_layout.Variables.Count > 0) {
                _stackWorkOffset = _layout.Variables.Max(kvp => kvp.Value) + 1;    
            }
            _maxStackWorkOffset = _stackWorkOffset;
        }

        public int AllocTemporaryStackAddress(int length = 1)
        {
            int address = _stackWorkOffset;
            _stackWorkOffset += length;
            _maxStackWorkOffset = Math.Max(_stackWorkOffset, _maxStackWorkOffset);
            return address;
        }

        public int FreeTemporaryStackAddress(int length = 1)
        {
            _stackWorkOffset -= length;
            return _stackWorkOffset;
        }

        public ILFunction MakeFunction()
        {
            return new ILFunction {
                ArgumentMemorySize = _argumentCount,
                MaxStackSize = _maxStackWorkOffset,
                Code = _instructions.ToArray(),
                Symbols = _addressToTokenMap,
                StringInstructions = _stringInstructions.ToArray(),
                VariableSymbols = _layout.Variables.ToDictionary(kvp => kvp.Value, kvp => kvp.Key),
                InstructionsReferencingFunctionAddresses = _instructionsReferencingFunctionAddresses.ToArray()
            };
        }

        private int AddInstruction(OpCode op, int immediate = 0)
        {
            int index = _instructions.Count;
            _instructions.Add(new Instruction(op, immediate));
            return index;
        }

        private void AddDebugSymbol(int address, IASTLeaf associatedLeaf)
        {
            Symbol range;
            _leafToTokenMap.TryGetValue(associatedLeaf, out range);
            _addressToTokenMap[address] = range;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            // Evaluate right hand side and put it in the stack.
            int rhsStackAddress = AllocTemporaryStackAddress();
            leaf.RHS.Accept(this);
            AddInstruction(OpCode.WRITE, rhsStackAddress);
            
            // Evaluate left hand side and keep it in the a register
            leaf.LHS.Accept(this);

            int comparisonOperationAddress = _instructions.Count;
            
            switch (leaf.Op.Value) {
                case ">":
                case "<":
                case Syntax.GREATER_THAN_OR_EQUAL_TO:
                case Syntax.LESS_THAN_OR_EQUAL_TO:
                    GenerateRelationalComparison(leaf.Op, rhsStackAddress);
                    break;
                case Syntax.EQUALS:
                    AddInstruction(OpCode.CMPE, rhsStackAddress);
                    break;
                case Syntax.NOT_EQUALS:
                    AddInstruction(OpCode.CMPE, rhsStackAddress);
                    AddInstruction(OpCode.LNOT);
                    break;
                case Syntax.LOGICAL_OR:
                    AddInstruction(OpCode.OR, rhsStackAddress);
                    AddInstruction(OpCode.BOOL);
                    break;
                case Syntax.LOGICAL_AND:
                    AddInstruction(OpCode.AND, rhsStackAddress);
                    AddInstruction(OpCode.BOOL);
                    break;
                case "+":
                    AddInstruction(OpCode.ADD, rhsStackAddress);
                    break;
                case "-":
                    AddInstruction(OpCode.SUB, rhsStackAddress);
                    break;
                case "*":
                    AddInstruction(OpCode.MUL, rhsStackAddress);
                    break;
                case "/":
                    AddInstruction(OpCode.DIV, rhsStackAddress);
                    break;
                case "%":
                    AddInstruction(OpCode.MOD, rhsStackAddress);
                    break;
                default:
                    throw new NotImplementedException(leaf.Op.Value);
            }

            FreeTemporaryStackAddress();
            
            AddDebugSymbol(comparisonOperationAddress, leaf);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            switch (leaf.Operator.Value) {
                case "-":
                    int rhsStackAddress = AllocTemporaryStackAddress();
                    leaf.RHS.Accept(this);
                    int addr = AddInstruction(OpCode.WRITE, rhsStackAddress);
                    AddInstruction(OpCode.LOAD, 0);
                    AddInstruction(OpCode.SUB, rhsStackAddress);
                    FreeTemporaryStackAddress();
                    AddDebugSymbol(addr, leaf);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void GenerateRelationalComparison(Token token, int rhsStackAddress)
        {
            if (token.Value.Contains("=")) {
                AddInstruction(OpCode.CMPLTE, rhsStackAddress);
            } else {
                AddInstruction(OpCode.CMPLT, rhsStackAddress);
            }

            if (token.Value.Contains(">")) {
                AddInstruction(OpCode.LNOT);
            }
        }
        
        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS.Accept(this);
            int variableAddress;
            _layout.Variables.TryGetValue(leaf.Declaration, out variableAddress);
            int addr = AddInstruction(OpCode.WRITE, variableAddress);
            AddDebugSymbol(addr, leaf);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            throw new InvalidOperationException("Enum leaf shouldn't be part of a function AST");
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            int value = leaf.Enum.Enumerations.First(kvp => kvp.Key == leaf.Name).Value;
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
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation - 1);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation - 1);
            
            int breakJumpLocation = _instructions.Count;

            foreach (int breakLocation in _breaks) {
                _instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation - breakLocation - 1);
            }
            _breaks.Clear();
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            int argumentStackLength = leaf.ArgumentCount + 1;
            int argumentStack = AllocTemporaryStackAddress(argumentStackLength);

            // First argument is the index of the function to be called
            int functionLoadAddr = AddInstruction(OpCode.LOAD, _functionManager.GetFunctionIndex(leaf.LHS));
            AddInstruction(OpCode.WRITE, argumentStack);
            
            _instructionsReferencingFunctionAddresses.Add(functionLoadAddr);
            
            // The rest of the argument stack is the argument values.
            int argumentStackValuesStart = argumentStack + 1;
            
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
                AddInstruction(OpCode.WRITE, argumentStackValuesStart + i);
            }
            
            int addr = AddInstruction(OpCode.CALL, argumentStack);
            
            FreeTemporaryStackAddress(argumentStackLength);
            
            // Add debug symbol at the first instruction that starts preparing the function to be called.
            AddDebugSymbol(functionLoadAddr, leaf);
        }

        private bool _hasVisitedFunctionDefinition;
        
        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            if (_hasVisitedFunctionDefinition) {
                // This should only be called once, for the function definition being generated.
                throw new InvalidOperationException("Attempting to visit another function definition.");
            }
            _hasVisitedFunctionDefinition = true;

            _argumentCount = leaf.Parameters.Length;
            
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
            int startAddress = _instructions.Count;
            AddDebugSymbol(startAddress, leaf.Condition);
            
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
            int index = _strings.Count;
            _strings.Add(leaf.Value);
            int addr = AddInstruction(OpCode.LOAD, index);
            _stringInstructions.Add(addr);
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
            AddInstruction(OpCode.JUMPNZ, bodyLocation - currentLocation - 1);
            
            _instructions[initialJumpLocation] = new Instruction(OpCode.JUMP, conditionLocation - initialJumpLocation - 1);

            int breakJumpLocation = _instructions.Count;

            foreach (int breakLocation in _breaks) {
                _instructions[breakLocation] = new Instruction(OpCode.JUMP, breakJumpLocation - breakLocation - 1);
            }
            _breaks.Clear();
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