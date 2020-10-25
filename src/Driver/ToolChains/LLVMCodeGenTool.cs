using System.IO;
using MonC;
using MonC.LLVM;

namespace Driver.ToolChains
{
    public class LLVMCodeGenTool : IModuleTool, IBackendInput
    {
        private readonly Job _job;
        private readonly LLVM _toolchain;
        private readonly ICodeGenInput _input;

        private LLVMCodeGenTool(Job job, LLVM toolchain, ICodeGenInput input)
        {
            _job = job;
            _toolchain = toolchain;
            _input = input;
        }

        public static LLVMCodeGenTool Construct(Job job, LLVM toolchain, ICodeGenInput input) =>
            new LLVMCodeGenTool(job, toolchain, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -LLVMCodeGenTool");
        }

        public void RunHeaderPass() => _input.RunHeaderPass();

        public void RunAnalyserPass() => _input.RunAnalyserPass();

        public IModuleArtifact GetModuleArtifact() =>
            _toolchain.CreateModule(_input.GetFileInfo(), _input.GetSemanticModule(), _job._semanticAnalyzer.Context);
    }
}
