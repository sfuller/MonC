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
            int delta = op switch {
                OpCode.NOOP => 0,
                OpCode.BREAK => 0,
                OpCode.PUSHWORD => sizeof(int),
                OpCode.PUSH => size,
                OpCode.POP => -size,
                OpCode.READ => size,
                OpCode.WRITE => 0,
                OpCode.ACCESS => -immediate,
                OpCode.ADDRESSOF => IntPtr.Size,
                OpCode.DEREF => -IntPtr.Size,
                OpCode.CALL => 0, // Space must be adjusted manually
                OpCode.RETURN => 0,
                OpCode.CMPE => -sizeof(int),
                OpCode.CMPLT => -sizeof(int),
                OpCode.CMPLTE => -sizeof(int),
                OpCode.JUMP => 0,
                OpCode.JUMPZ => -sizeof(int),
                OpCode.JUMPNZ => -sizeof(int),
                OpCode.BOOL => 0,
                OpCode.LNOT => 0,
                OpCode.ADD => -sizeof(int),
                OpCode.SUB => -sizeof(int),
                OpCode.OR => -sizeof(int),
                OpCode.AND => -sizeof(int),
                OpCode.XOR => -sizeof(int),
                OpCode.MUL => -sizeof(int),
                OpCode.DIV => -sizeof(int),
                OpCode.MOD => -sizeof(int),
                _ => throw new NotSupportedException()
            };

            if (delta > 0) {
                AllocStackSpace(delta);
            } else {
                FreeStackSpace(delta);
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
