using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public abstract class PassManager : IDisposable
    {
        protected LLVMPassManagerRef _passManager;

        public static implicit operator LLVMPassManagerRef(PassManager passManager) => passManager._passManager;

        protected PassManager(LLVMPassManagerRef passManager) => _passManager = passManager;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            _passManager.Dispose();
        }

        ~PassManager() => DoDispose();
    }

    public class ModulePassManager : PassManager
    {
        public ModulePassManager() : base(LLVMPassManagerRef.Create()) { }

        public ModulePassManager(PassManagerBuilder optBuilder) : base(LLVMPassManagerRef.Create()) =>
            optBuilder.PopulateModulePassManager(this);

        public bool Run(Module m) => _passManager.Run(m);
    }

    public sealed class LTOPassManager : ModulePassManager
    {
        public LTOPassManager() { }

        public LTOPassManager(PassManagerBuilder optBuilder, bool internalize, bool runInliner) =>
            optBuilder.PopulateLTOPassManager(this, internalize, runInliner);
    }

    public sealed class FunctionPassManager : PassManager
    {
        public FunctionPassManager(Module module) : base(((LLVMModuleRef) module).CreateFunctionPassManager()) { }

        public FunctionPassManager(Module module, PassManagerBuilder optBuilder) : base(((LLVMModuleRef) module)
            .CreateFunctionPassManager()) => optBuilder.PopulateFunctionPassManager(this);

        public bool Initialize() => _passManager.InitializeFunctionPassManager();

        public bool Run(Value f) => _passManager.RunFunctionPassManager(f);

        public bool FinalizeFunctionPassManager() => _passManager.FinalizeFunctionPassManager();
    }
}
