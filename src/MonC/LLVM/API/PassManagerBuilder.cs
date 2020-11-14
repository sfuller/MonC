using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class PassManagerBuilder : IDisposable
    {
        private LLVMPassManagerBuilderRef _passManagerBuilder;

        public static implicit operator LLVMPassManagerBuilderRef(PassManagerBuilder passManagerBuilder) =>
            passManagerBuilder._passManagerBuilder;

        public unsafe PassManagerBuilder() => _passManagerBuilder = LLVMSharp.Interop.LLVM.PassManagerBuilderCreate();

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            _passManagerBuilder.Dispose();
        }

        ~PassManagerBuilder() => DoDispose();

        public void SetOptLevel(uint optLevel) => _passManagerBuilder.SetOptLevel(optLevel);

        public void SetSizeLevel(uint sizeLevel) => _passManagerBuilder.SetSizeLevel(sizeLevel);

        public void SetDisableUnrollLoops(bool value) => _passManagerBuilder.SetDisableUnrollLoops(value ? 1 : 0);

        public void UseInlinerWithThreshold(uint threshold) => _passManagerBuilder.UseInlinerWithThreshold(threshold);

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
            _passManagerBuilder.PopulateFunctionPassManager(pm);

        public void PopulateModulePassManager(ModulePassManager pm) =>
            _passManagerBuilder.PopulateModulePassManager(pm);

        public void PopulateLTOPassManager(LTOPassManager pm, bool internalize, bool runInliner) =>
            _passManagerBuilder.PopulateLTOPassManager(pm, internalize ? 1 : 0, runInliner ? 1 : 0);
    }
}
