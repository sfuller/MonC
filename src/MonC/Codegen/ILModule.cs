using System.Collections.Generic;
using MonC.Bytecode;

namespace MonC.Codegen
{
    public struct ILFunction
    {
        public Instruction[] Code;
        public IDictionary<int, Symbol> Symbols;

        /// <summary>
        /// Indices of all instructions which use a string as their value.
        /// </summary>
        public int[] StringInstructions;
    }
    
    public class ILModule
    {
        public ILFunction[] DefinedFunctions;
        public string[] UndefinedFunctionNames;
        public KeyValuePair<string, int>[] ExportedFunctions;
        public KeyValuePair<string, int>[] ExportedEnumValues;
        public string[] Strings;
    }
}