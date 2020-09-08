using System;
using System.Collections.Generic;
using System.Linq;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class FunctionBuilder
    {
        private readonly FunctionStackLayout _layout;
        private readonly IDictionary<ISyntaxTreeLeaf, Symbol> _leafToTokenMap;

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

        public FunctionBuilder(FunctionStackLayout layout, IDictionary<ISyntaxTreeLeaf, Symbol> leafToTokenMap)
        {
            _layout = layout;
            _leafToTokenMap = leafToTokenMap;

            if (layout.Variables.Count > 0) {
                _stackWorkOffset = layout.Variables.Max(kvp => kvp.Value) + 1;
            }
            _maxStackWorkOffset = _stackWorkOffset;
        }

        public List<Instruction> Instructions => _instructions;
        public int InstructionCount => _instructions.Count;

        public int AddInstruction(OpCode op, int immediate = 0)
        {
            int index = _instructions.Count;
            _instructions.Add(new Instruction(op, immediate));
            return index;
        }

        public void AddDebugSymbol(int address, ISyntaxTreeLeaf associatedLeaf)
        {
            Symbol range;
            _leafToTokenMap.TryGetValue(associatedLeaf, out range);
            _addressToTokenMap[address] = range;
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

        public void SetInstructionReferencingFunctionAddress(int instructionIndex)
        {
            _instructionsReferencingFunctionAddresses.Add(instructionIndex);
        }

        public void SetStringInstruction(int instructionIndex)
        {
            _stringInstructions.Add(instructionIndex);
        }

        public ILFunction Build(FunctionDefinitionLeaf functionDefinitionLeaf)
        {
            // Note: Will need to be adjusted when non sizeof(int) sized types are introduced.
            int argumentMemorySize = functionDefinitionLeaf.Parameters.Length;

            return new ILFunction {
                ArgumentMemorySize = argumentMemorySize,
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