using System.Collections.Generic;
using MonC.Bytecode;

namespace MonC.Codegen
{
    public class ILModule
    {
        public Instruction[][] DefinedFunctions;
        public Dictionary<string, int> DefinedFunctionsIndices;
        public Dictionary<string, int> UndefinedFunctionsIndices = new Dictionary<string, int>();
        public string[] ExportedFunctions;
    }
}