using System.IO;
using MonC;

namespace Driver.ToolChains
{
    public class LLVMLinkerInputFileTool : IModuleTool
    {
        private LLVM _toolchain;
        private FileInfo _fileInfo;

        private LLVMLinkerInputFileTool(LLVM toolchain, FileInfo fileInfo)
        {
            _toolchain = toolchain;
            _fileInfo = fileInfo;
        }

        public static LLVMLinkerInputFileTool Construct(Job job, LLVM toolchain, FileInfo fileInfo) =>
            new LLVMLinkerInputFileTool(toolchain, fileInfo);

        public void WriteInputChain(TextWriter writer)
        {
            _fileInfo.WriteInputChain(writer);
            writer.WriteLine("  -LLVMLinkerInputFileTool");
        }

        public void RunHeaderPass()
        {
            throw new System.NotImplementedException();
        }

        public IModuleArtifact GetModuleArtifact() => _toolchain.ParseIR(_fileInfo);
    }
}
