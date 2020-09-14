using System;
using System.Collections.Generic;
using System.IO;
using MonC;
using MonC.LLVM;

namespace Driver.ToolChains
{
    public class LLVMLinkTool : IExecutableTool, IVMInput
    {
        private Job _job;
        private ILinkInput _input;

        private LLVMLinkTool(Job job, ILinkInput input)
        {
            _job = job;
            _input = input;
        }

        public static LLVMLinkTool Construct(Job job, LLVM toolchain, ILinkInput input) => new LLVMLinkTool(job, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -LLVMLinkTool");
        }

        public int Execute()
        {
            // TODO: This should serialize a fully linked module to _job._outputFile
            return 0;
        }

        public IVMModuleArtifact GetVMModuleArtifact()
        {
            List<Module> modules = _input.GetModuleArtifacts().ConvertAll(m => (Module) m);

            ExecutionEngine ee = null;
            foreach (Module module in modules) {
                if (ee == null)
                    ee = ExecutionEngine.CreateForModule(module);
                else
                    ee.AddModule(module);
            }

            return ee;
        }
    }
}
