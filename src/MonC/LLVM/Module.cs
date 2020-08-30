using System;

namespace MonC.LLVM
{
    public sealed class Module : IDisposable
    {
        private CAPI.LLVMModuleRef _module;
        public DIBuilder DiBuilder { get; }
        public bool IsValid => _module.IsValid;

        internal Module(string name, CAPI.LLVMContextRef context)
        {
            _module = CAPI.LLVMModuleCreateWithNameInContext(name, context);
            DiBuilder = new DIBuilder(_module);
        }

        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        private void DoDispose(bool disposing)
        {
            if (_module.IsValid) {
                if (disposing)
                    DiBuilder.Dispose();
                CAPI.LLVMDisposeModule(_module);
                _module = new CAPI.LLVMModuleRef();
            }
        }

        ~Module()
        {
            DoDispose(false);
        }

        public Value AddFunction(string name, Type functionTy) =>
            new Value(CAPI.LLVMAddFunction(_module, name, functionTy));
        
        public void Dump()
        {
            CAPI.LLVMDumpModule(_module);
        }
    }
}