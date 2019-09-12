using System.Collections.Generic;
using MonC.Bytecode;

namespace MonC.Codegen
{
    public struct ILFunction
    {
        public Instruction[] Code;
        public IDictionary<int, TokenRange> Symbols;
    }
    
    public class ILModule
    {
        public ILFunction[] DefinedFunctions;
        public string[] UndefinedFunctionNames;
        public KeyValuePair<string, int>[] ExportedFunctions;
    }
}