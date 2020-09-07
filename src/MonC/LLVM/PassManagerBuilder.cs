using System;

namespace MonC.LLVM
{
    public class PassManagerBuilder : IDisposable
    {
        private CAPI.LLVMPassManagerBuilderRef _passManagerBuilder;

        public static implicit operator CAPI.LLVMPassManagerBuilderRef(PassManagerBuilder passManagerBuilder) =>
            passManagerBuilder._passManagerBuilder;

        public PassManagerBuilder() => _passManagerBuilder = CAPI.LLVMPassManagerBuilderCreate();

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_passManagerBuilder.IsValid) {
                CAPI.LLVMPassManagerBuilderDispose(_passManagerBuilder);
                _passManagerBuilder = new CAPI.LLVMPassManagerBuilderRef();
            }
        }

        ~PassManagerBuilder() => DoDispose();

        public void SetOptLevel(uint optLevel) => CAPI.LLVMPassManagerBuilderSetOptLevel(_passManagerBuilder, optLevel);

        public void SetSizeLevel(uint sizeLevel) =>
            CAPI.LLVMPassManagerBuilderSetSizeLevel(_passManagerBuilder, sizeLevel);

        public void SetDisableUnrollLoops(bool value) =>
            CAPI.LLVMPassManagerBuilderSetDisableUnrollLoops(_passManagerBuilder, value);

        public void UseInlinerWithThreshold(uint threshold) =>
            CAPI.LLVMPassManagerBuilderUseInlinerWithThreshold(_passManagerBuilder, threshold);

        private uint ComputeThresholdFromOptLevels(uint optLevel, uint sizeLevel)
        {
            if (optLevel > 2)
                return 250;
            if (sizeLevel == 1) // -Os
                return 50;
            if (sizeLevel == 2) // -Oz
                return 5;
            return 225;
        }

        public void UseInlinerWithOptLevels(uint optLevel, uint sizeLevel) =>
            UseInlinerWithThreshold(ComputeThresholdFromOptLevels(optLevel, sizeLevel));

        public void SetOptLevels(uint optLevel, uint sizeLevel)
        {
            SetOptLevel(optLevel);
            SetSizeLevel(sizeLevel);
            UseInlinerWithOptLevels(optLevel, sizeLevel);
        }

        public void PopulateFunctionPassManager(FunctionPassManager pm) =>
            CAPI.LLVMPassManagerBuilderPopulateFunctionPassManager(_passManagerBuilder, pm);

        public void PopulateModulePassManager(ModulePassManager pm) =>
            CAPI.LLVMPassManagerBuilderPopulateModulePassManager(_passManagerBuilder, pm);

        public void PopulateLTOPassManager(LTOPassManager pm, bool internalize, bool runInliner) =>
            CAPI.LLVMPassManagerBuilderPopulateLTOPassManager(_passManagerBuilder, pm, internalize, runInliner);
    }
}