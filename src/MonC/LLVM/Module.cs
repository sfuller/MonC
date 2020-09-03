using System;

namespace MonC.LLVM
{
    public sealed class Module : IDisposable
    {
        private CAPI.LLVMModuleRef _module;
        public DIBuilder? DiBuilder { get; }
        public bool IsValid => _module.IsValid;

        public static implicit operator CAPI.LLVMModuleRef(Module module) => module._module;

        internal Module(string name, CAPI.LLVMContextRef context, bool debugInfo)
        {
            _module = CAPI.LLVMModuleCreateWithNameInContext(name, context);
            if (debugInfo) {
                DiBuilder = new DIBuilder(_module);
            }
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
                    DiBuilder?.Dispose();
                }

                CAPI.LLVMDisposeModule(_module);
                _module = new CAPI.LLVMModuleRef();
            }
        }

        ~Module() => DoDispose(false);

        public void AddModuleFlag(CAPI.LLVMModuleFlagBehavior behavior, string key, Metadata val) =>
            CAPI.LLVMAddModuleFlag(_module, behavior, key, val);

        public string Target => CAPI.LLVMGetTarget(_module);

        public void SetTarget(string triple) => CAPI.LLVMSetTarget(_module, triple);

        public Value AddFunction(string name, Type functionTy) => CAPI.LLVMAddFunction(_module, name, functionTy);

        public void Dump() => CAPI.LLVMDumpModule(_module);
    }
}