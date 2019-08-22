using System.Collections.Generic;
using MonC.Bytecode;

namespace MonC.Codegen
{
    public class ILModule
    {
        public Instruction[][] DefinedFunctions;
        public string[] UndefinedFunctionNames;
        public KeyValuePair<string, int>[] ExportedFunctions;
    }
}