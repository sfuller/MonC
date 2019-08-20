using System.Collections.Generic;
using System.IO;
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

            foreach (KeyValuePair<string, int> pair in module.DefinedFunctionsIndices) {
                WriteFunction(pair.Key, pair.Value, module.DefinedFunctions[pair.Value]);
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