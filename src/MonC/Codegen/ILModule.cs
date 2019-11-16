using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public struct ILFunction
    {
        public Instruction[] Code;
        public IDictionary<int, Symbol> Symbols;
        public Dictionary<int, DeclarationLeaf> VariableSymbols;

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
                VariableSymbols = new Dictionary<int, DeclarationLeaf>() 
            };
        } 
    }
    
    public class ILModule
    {
        public ILFunction[] DefinedFunctions = new ILFunction[0];
        public string[] UndefinedFunctionNames = new string[0];
        public KeyValuePair<string, int>[] ExportedFunctions = new KeyValuePair<string, int>[0];
        public KeyValuePair<string, int>[] ExportedEnumValues = new KeyValuePair<string, int>[0];
        public string[] Strings = new string[0];
    }
}