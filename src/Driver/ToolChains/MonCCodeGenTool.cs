using System.IO;

namespace Driver.ToolChains
{
    public class MonCCodeGenTool : IExecutableTool, ILinkInput
    {
        private MonC _toolchain;
        private ICodeGenInput _input;

        private MonCCodeGenTool(MonC toolchain, ICodeGenInput input)
        {
            _toolchain = toolchain;
            _input = input;
        }

        public static MonCCodeGenTool Construct(Job job, MonC toolchain, ICodeGenInput input) =>
            new MonCCodeGenTool(toolchain, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCCodeGenTool");
        }

        public void Execute() => throw new System.NotImplementedException();
    }
}