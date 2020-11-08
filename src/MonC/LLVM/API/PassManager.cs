using System;

namespace MonC.LLVM
{
    public abstract class PassManager : IDisposable
    {
        protected CAPI.LLVMPassManagerRef _passManager;

        public static implicit operator CAPI.LLVMPassManagerRef(PassManager passManager) => passManager._passManager;

        protected PassManager(CAPI.LLVMPassManagerRef passManager) => _passManager = passManager;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_passManager.IsValid) {
                CAPI.LLVMDisposePassManager(_passManager);
                _passManager = new CAPI.LLVMPassManagerRef();
            }
        }

        ~PassManager() => DoDispose();
    }

    public class ModulePassManager : PassManager
    {
        public ModulePassManager() : base(CAPI.LLVMCreatePassManager()) { }

        public ModulePassManager(PassManagerBuilder optBuilder) : base(CAPI.LLVMCreatePassManager()) =>
            optBuilder.PopulateModulePassManager(this);

        public bool Run(Module m) => CAPI.LLVMRunPassManager(_passManager, m);
    }

    public sealed class LTOPassManager : ModulePassManager
    {
        public LTOPassManager() { }

        public LTOPassManager(PassManagerBuilder optBuilder, bool internalize, bool runInliner) =>
            optBuilder.PopulateLTOPassManager(this, internalize, runInliner);
    }

    public sealed class FunctionPassManager : PassManager
    {
        public FunctionPassManager(Module module) : base(CAPI.LLVMCreateFunctionPassManagerForModule(module)) { }

        public FunctionPassManager(Module module, PassManagerBuilder optBuilder) : base(
            CAPI.LLVMCreateFunctionPassManagerForModule(module)) => optBuilder.PopulateFunctionPassManager(this);

        public bool Initialize() => CAPI.LLVMInitializeFunctionPassManager(_passManager);

        public bool Run(Value f) => CAPI.LLVMRunFunctionPassManager(_passManager, f);

        public bool FinalizeFunctionPassManager() => CAPI.LLVMFinalizeFunctionPassManager(_passManager);
    }
}
