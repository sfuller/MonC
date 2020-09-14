using System.IO;
using MonC;
using MonC.LLVM;

namespace Driver.ToolChains
{
    public class LLVMBackendTool : IModuleTool
    {
        private LLVM _toolchain;
        private IBackendInput _input;

        private LLVMBackendTool(LLVM toolchain, IBackendInput input)
        {
            _toolchain = toolchain;
            _input = input;
        }

        public static LLVMBackendTool Construct(Job job, LLVM toolchain, IBackendInput input) =>
            new LLVMBackendTool(toolchain, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -LLVMBackendTool");
        }

        public IModuleArtifact GetModuleArtifact() =>
            new LLVMNativeModule(_toolchain, (Module) _input.GetModuleArtifact());
    }
}
