using System;
using System.IO;
using MonC;
using MonC.Codegen;
using MonC.Frontend;

namespace Driver.ToolChains
{
    public class MonCCodeGenTool : IModuleTool
    {
        private ICodeGenInput _input;

        private MonCCodeGenTool(ICodeGenInput input) => _input = input;

        public static MonCCodeGenTool Construct(Job job, MonC toolchain, ICodeGenInput input) =>
            new MonCCodeGenTool(input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCCodeGenTool");
        }

        public void RunHeaderPass() => _input.RunHeaderPass();

        public IModuleArtifact GetModuleArtifact()
        {
            CodeGenerator generator = new CodeGenerator();
            return generator.Generate(_input.GetParseModule());
        }
    }
}
