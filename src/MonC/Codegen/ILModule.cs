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
        public ILFunction[] DefinedFunctions = new ILFunction[0];
        public string[] UndefinedFunctionNames = new string[0];
        public KeyValuePair<string, int>[] ExportedFunctions = new KeyValuePair<string, int>[0];
        public KeyValuePair<string, int>[] ExportedEnumValues = new KeyValuePair<string, int>[0];
        public string[] Strings = new string[0];
    }
}