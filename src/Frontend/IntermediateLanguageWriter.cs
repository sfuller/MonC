using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonC.Bytecode;
using MonC.Codegen;

namespace MonC.Frontend
{
    public class IntermediateLanguageWriter
    {
        private TextWriter _writer;

        public void Write(ILModule module, TextWriter writer)
        {
            _writer = writer;

            Dictionary<int, string> exportedFunctionNames = module.ExportedFunctions.ToDictionary(x => x.Value, x => x.Key);
            
            for (int i = 0, ilen = module.DefinedFunctions.Length; i < ilen; ++i) {
                string name;
                if (!exportedFunctionNames.TryGetValue(i, out name)) {
                    name = "";
                }
                WriteFunction(name, i, module.DefinedFunctions[i].Code);
            }
        }

        public void WriteFunction(string name, int index, Instruction[] instructions)
        {
            _writer.WriteLine($"[{index}] {name}");
            foreach (Instruction instruction in instructions) {
                _writer.WriteLine($"\t{instruction.Op} \t{instruction.ImmediateValue}");    
            }
            _writer.WriteLine();
        }
        
    }
}