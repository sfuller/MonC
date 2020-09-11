using System;
using System.IO;

namespace MonC.LLVM
{
    public sealed class Module : IModuleArtifact
    {
        private CAPI.LLVMModuleRef _module;
        private Context _parent;
        public DIBuilder DiBuilder { get; }
        public bool IsValid => _module.IsValid;

        public static implicit operator CAPI.LLVMModuleRef(Module module) => module._module;

        internal Module(string name, Context context)
        {
            _module = CAPI.LLVMModuleCreateWithNameInContext(name, context);
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
            if (_module.IsValid) {
                if (disposing) {
                    DiBuilder.Dispose();
                }

                CAPI.LLVMDisposeModule(_module);
                _module = new CAPI.LLVMModuleRef();
                _parent.DecrementModule();
            }
        }

        ~Module() => DoDispose(false);

        public void AddModuleFlag(CAPI.LLVMModuleFlagBehavior behavior, string key, Metadata val) =>
            CAPI.LLVMAddModuleFlag(_module, behavior, key, val);

        public string Target => CAPI.LLVMGetTarget(_module);

        public void SetTarget(string triple) => CAPI.LLVMSetTarget(_module, triple);

        public Value AddFunction(string name, Type functionTy) => CAPI.LLVMAddFunction(_module, name, functionTy);

        public void Dump() => CAPI.LLVMDumpModule(_module);

        public bool PrintToFile(string filename, out string? errorMessage) =>
            CAPI.LLVMPrintModuleToFile(_module, filename, out errorMessage);

        public string PrintToString() => CAPI.LLVMPrintModuleToStringPublic(_module);

        public void WriteListing(TextWriter writer)
        {
            // If the writer is determined to be a file writer, use LLVM's native file I/O
            if (writer is StreamWriter streamWriter) {
                if (streamWriter.BaseStream is FileStream fileStream) {
                    writer.Flush();
                    PrintToFile(fileStream.Name, out string? errorMessage);
                    return;
                }
            }

            // Otherwise write a giant string
            writer.Write(PrintToString());
        }
    }
}
