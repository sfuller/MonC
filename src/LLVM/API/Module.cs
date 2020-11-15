using System;
using System.IO;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public sealed class Module : IModuleArtifact
    {
        private LLVMModuleRef _module;
        private Context _parent;
        public DIBuilder DiBuilder { get; }
        public bool IsValid => _module.Handle != IntPtr.Zero;

        internal void Release()
        {
            DiBuilder.Dispose();
            _module = new LLVMModuleRef();
            _parent.DecrementModule();
        }

        public static implicit operator LLVMModuleRef(Module module) => module._module;

        internal Module(string name, Context context)
        {
            _module = ((LLVMContextRef) context).CreateModuleWithName(name);
            _parent = context;
            DiBuilder = new DIBuilder(_module);
            _parent.IncrementModule();
        }

        internal Module(LLVMModuleRef module, Context context)
        {
            _module = module;
            _parent = context;
            DiBuilder = new DIBuilder(_module);
            _parent.IncrementModule();
        }

        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        private void DoDispose(bool disposing)
        {
            if (_module.Handle != IntPtr.Zero) {
                if (disposing) {
                    DiBuilder.Dispose();
                }

                _module.Dispose();
                _parent.DecrementModule();
            }
        }

        ~Module() => DoDispose(false);

        public unsafe void AddModuleFlag(LLVMModuleFlagBehavior behavior, string key, Metadata val)
        {
            using var marshaledKey = new MarshaledString(key.AsSpan());
            LLVMSharp.Interop.LLVM.AddModuleFlag(_module, behavior, marshaledKey, (UIntPtr) key.Length,
                (LLVMMetadataRef) val);
        }

        public string Target => _module.Target;

        public void SetTarget(string triple) => _module.Target = triple;

        public Value AddFunction(string name, Type functionTy) => _module.AddFunction(name, functionTy);

        public unsafe bool LinkInModule(Module other)
        {
            bool error = LLVMSharp.Interop.LLVM.LinkModules2(_module, (LLVMModuleRef) other) != 0;
            other.Release();
            return error;
        }

        public void Dump() => _module.Dump();

        public void PrintToFile(string filename) => _module.PrintToFile(filename);

        public string PrintToString() => _module.PrintToString();

        public int WriteBitcodeToFile(string path) => _module.WriteBitcodeToFile(path);

        public MemoryBuffer WriteBitcodeToMemoryBuffer() =>
            new MemoryBuffer(_module.WriteBitcodeToMemoryBuffer());

        public void WriteListing(TextWriter writer)
        {
            // If the writer is determined to be a file writer, use LLVM's native file I/O
            if (writer is StreamWriter streamWriter) {
                if (streamWriter.BaseStream is FileStream fileStream) {
                    string path = fileStream.Name;
                    writer.Close();
                    PrintToFile(path);
                    return;
                }
            }

            // Otherwise write a giant string
            writer.Write(PrintToString());
        }

        public void WriteRelocatable(BinaryWriter writer)
        {
            // If the writer is determined to be a file writer, use LLVM's native file I/O
            if (writer.BaseStream is FileStream fileStream) {
                string path = fileStream.Name;
                writer.Close();
                WriteBitcodeToFile(path);
                return;
            }

            // Otherwise write a giant buffer
            using MemoryBuffer memBuf = WriteBitcodeToMemoryBuffer();
            writer.Write(memBuf.Bytes);
        }
    }
}
