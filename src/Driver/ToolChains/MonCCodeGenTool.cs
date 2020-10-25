using System;
using System.IO;
using MonC;
using MonC.Codegen;
using MonC.Frontend;

namespace Driver.ToolChains
{
    public class MonCCodeGenTool : IModuleTool
    {
        private readonly Job _job;
        private readonly ICodeGenInput _input;

        private MonCCodeGenTool(Job job, ICodeGenInput input)
        {
            _job = job;
            _input = input;
        }

        public static MonCCodeGenTool Construct(Job job, MonC toolchain, ICodeGenInput input) =>
            new MonCCodeGenTool(job, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCCodeGenTool");
        }

        public void RunHeaderPass() => _input.RunHeaderPass();

        public void RunAnalyserPass() => _input.RunAnalyserPass();

        public IModuleArtifact GetModuleArtifact()
        {
            CodeGenerator generator = new CodeGenerator(_input.GetSemanticModule(), _job._semanticAnalyzer.Context);
            return generator.Generate();
        }
    }
}
