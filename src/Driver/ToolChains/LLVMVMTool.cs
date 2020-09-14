using System;
using System.IO;
using MonC.LLVM;

namespace Driver.ToolChains
{
    public class LLVMVMTool : IExecutableTool
    {
        private Job _job;
        private LLVM _toolchain;
        private IVMInput _input;

        private LLVMVMTool(Job job, LLVM toolchain, IVMInput input)
        {
            _job = job;
            _toolchain = toolchain;
            _input = input;
        }

        public static LLVMVMTool Construct(Job job, LLVM toolchain, IVMInput input) =>
            new LLVMVMTool(job, toolchain, input);

        public virtual void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -LLVMVMTool");
        }

        public int Execute()
        {
            ExecutionEngine executionEngine = (ExecutionEngine) _input.GetVMModuleArtifact();
            Value mainFunc = executionEngine.FindFunction("main");
            if (!mainFunc.IsValid) {
                throw Diagnostics.ThrowError("Unable to find main function in VM modules");
            }

            GenericValue[] args = new GenericValue[_job._argsToPass.Count];
            for (int i = 0, iend = _job._argsToPass.Count; i < iend; ++i) {
                args[i] = _toolchain.CreateIntGenericValue(_job._argsToPass[i]);
            }

            using GenericValue retVal = executionEngine.RunFunction(mainFunc, args);

            foreach (GenericValue arg in args) {
                arg.Dispose();
            }

            return (int) retVal.IntValue(true);
        }
    }
}
