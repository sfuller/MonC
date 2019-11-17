using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public struct ILFunction
    {
        /// <summary>
        /// The size of the argument stack prepared by the caller.
        /// This is needed so the VM knowns how much memory to copy from the previous stack frame to the current one.
        /// Implementation specific and might not be needed for other implementations if they are smart enough to share
        /// the stack memory.  
        /// </summary>
        public int ArgumentMemorySize;

        /// <summary>
        /// The maximum ammount of stack memory that may possibly be used by this function.
        /// </summary>
        public int MaxStackSize;
        
        public Instruction[] Code;
        public IDictionary<int, Symbol> Symbols;
        public Dictionary<int, DeclarationLeaf> VariableSymbols;
        
        /// <summary>
        /// All indices of instructions that reference a function address. Used for linking.
        /// </summary>
        public int[] InstructionsReferencingFunctionAddresses;

        /// <summary>
        /// Indices of all instructions which use a string as their value.
        /// </summary>
        public int[] StringInstructions;

        public static ILFunction Empty()
        {
            return new ILFunction() {
                Code = Array.Empty<Instruction>(),
                Symbols = new Dictionary<int, Symbol>(),
                StringInstructions = Array.Empty<int>(),
                VariableSymbols = new Dictionary<int, DeclarationLeaf>(),
                InstructionsReferencingFunctionAddresses = Array.Empty<int>()
            };
        } 
    }

}