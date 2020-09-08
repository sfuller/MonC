using System.IO;
using MonC;
using MonC.LLVM;

namespace Driver.ToolChains
{
    public class LLVMCodeGenTool : IModuleTool, IBackendInput
    {
        private LLVM _toolchain;
        private ICodeGenInput _input;

        private LLVMCodeGenTool(LLVM toolchain, ICodeGenInput input)
        {
            _toolchain = toolchain;
            _input = input;
        }

        public static LLVMCodeGenTool Construct(Job job, LLVM toolchain, ICodeGenInput input) =>
            new LLVMCodeGenTool(toolchain, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -LLVMCodeGenTool");
        }

        public ModuleArtifact GetModuleArtifact() =>
            _toolchain.CreateModule(_input.GetFileInfo(), _input.GetParseModule());
    }
}