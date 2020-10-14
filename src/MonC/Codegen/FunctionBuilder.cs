using System;
using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class FunctionBuilder
    {
        private readonly FunctionStackLayout _layout;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _nodeToTokenMap;

        private readonly IDictionary<int, Symbol> _addressToTokenMap = new Dictionary<int, Symbol>();
        private readonly List<Instruction> _instructions = new List<Instruction>();
        private readonly List<int> _stringInstructions = new List<int>();
        private readonly List<int> _instructionsReferencingFunctionAddresses = new List<int>();

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

        public FunctionBuilder(FunctionStackLayout layout, IDictionary<ISyntaxTreeNode, Symbol> nodeToTokenMap)
        {
            _layout = layout;
            _nodeToTokenMap = nodeToTokenMap;
        }

        public List<Instruction> Instructions => _instructions;
        public int InstructionCount => _instructions.Count;

        public int AddInstruction(OpCode op, int immediate = 0, int size = 4)
        {
            switch (op) {
                case OpCode.NOOP:
                case OpCode.BREAK:
                    // No Stack Change
                    break;
                case OpCode.PUSHWORD:
                    AllocStackSpace(sizeof(int));
                    break;
                case OpCode.PUSH:
                    AllocStackSpace(size);
                    break;
                case OpCode.POP:
                    FreeStackSpace(size);
                    break;
                case OpCode.READ:
                    AllocStackSpace(size);
                    break;
                case OpCode.WRITE:
                    // No stack change
                    break;
                case OpCode.ACCESS:
                    FreeStackSpace(size - immediate);
                    break;
                case OpCode.CALL:
                    // Space must be adjusted manually
                    break;
                case OpCode.RETURN:
                    // No stack change
                    break;
                case OpCode.CMPE:
                case OpCode.CMPLT:
                case OpCode.CMPLTE:
                    FreeStackSpace(sizeof(int));
                    break;
                case OpCode.JUMP:
                    // No change
                    break;
                case OpCode.JUMPZ:
                case OpCode.JUMPNZ:
                    FreeStackSpace(sizeof(int));
                    break;
                case OpCode.BOOL:
                case OpCode.LNOT:
                    // No change
                    break;
                case OpCode.ADD:
                case OpCode.SUB:
                case OpCode.OR:
                case OpCode.AND:
                case OpCode.XOR:
                case OpCode.MUL:
                case OpCode.DIV:
                case OpCode.MOD:
                    FreeStackSpace(sizeof(int));
                    break;
                default:
                    throw new NotSupportedException();
            }

            int index = _instructions.Count;
            _instructions.Add(new Instruction(op, immediate, size));
            return index;
        }

        public void AddDebugSymbol(int address, ISyntaxTreeNode associatedNode)
        {
            Symbol range;
            _nodeToTokenMap.TryGetValue(associatedNode, out range);
            _addressToTokenMap[address] = range;
        }

        public int AllocStackSpace(int length = 1)
        {
            int address = _stackWorkOffset;
            _stackWorkOffset += length;
            _maxStackWorkOffset = Math.Max(_stackWorkOffset, _maxStackWorkOffset);
            return address;
        }

        public int FreeStackSpace(int length = 1)
        {
            if (_stackWorkOffset < length) {
                throw new InvalidOperationException("Stack work offset underflow");
            }
            _stackWorkOffset -= length;
            return _stackWorkOffset;
        }

        public void SetInstructionReferencingFunctionAddress(int instructionIndex)
        {
            _instructionsReferencingFunctionAddresses.Add(instructionIndex);
        }

        public void SetStringInstruction(int instructionIndex)
        {
            _stringInstructions.Add(instructionIndex);
        }

        public ILFunction Build(FunctionDefinitionNode functionDefinitionNode)
        {
            return new ILFunction {
                ArgumentMemorySize = _layout.ArgumentsSize,
                ReturnValueSize = _layout.ReturnValueSize,
                MaxStackSize = _maxStackWorkOffset,
                Code = _instructions.ToArray(),
                Symbols = _addressToTokenMap,
                StringInstructions = _stringInstructions.ToArray(),
                VariableSymbols = _layout.Variables.ToDictionary(kvp => kvp.Value, kvp => kvp.Key),
                InstructionsReferencingFunctionAddresses = _instructionsReferencingFunctionAddresses.ToArray()
            };
        }

    }
}
