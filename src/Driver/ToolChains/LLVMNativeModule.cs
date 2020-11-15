using System.IO;
using MonC;
using MonC.LLVM;
using LLVMSharp.Interop;

namespace Driver.ToolChains
{
    public class LLVMNativeModule : IModuleArtifact
    {
        private readonly LLVM _toolchain;
        private readonly Module _input;

        public LLVMNativeModule(LLVM toolchain, Module input)
        {
            _toolchain = toolchain;
            _input = input;
        }

        public void WriteListing(TextWriter writer)
        {
            // If the writer is determined to be a file writer, use LLVM's native file I/O
            if (writer is StreamWriter streamWriter) {
                if (streamWriter.BaseStream is FileStream fileStream) {
                    string path = fileStream.Name;
                    writer.Close();
                    _toolchain.TargetMachine.EmitToFile(_input, path, LLVMCodeGenFileType.LLVMAssemblyFile);
                    return;
                }
            }

            // Otherwise write a giant string
            using MemoryBuffer memBuf =
                _toolchain.TargetMachine.EmitToMemoryBuffer(_input, LLVMCodeGenFileType.LLVMAssemblyFile);
            writer.Write(memBuf.GetAsString());
        }

        public void WriteRelocatable(BinaryWriter writer)
        {
            // If the writer is determined to be a file writer, use LLVM's native file I/O
            if (writer.BaseStream is FileStream fileStream) {
                string path = fileStream.Name;
                writer.Close();
                _toolchain.TargetMachine.EmitToFile(_input, path, LLVMCodeGenFileType.LLVMObjectFile);
                return;
            }

            // Otherwise write a giant string
            using MemoryBuffer memBuf =
                _toolchain.TargetMachine.EmitToMemoryBuffer(_input, LLVMCodeGenFileType.LLVMObjectFile);
            writer.Write(memBuf.Bytes);
        }

        public void Dispose()
        {
            _input.Dispose();
        }
    }
}
