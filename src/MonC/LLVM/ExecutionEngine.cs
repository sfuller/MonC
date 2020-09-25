using System;

namespace MonC.LLVM
{
    public class ExecutionEngine : IVMModuleArtifact
    {
        private CAPI.LLVMExecutionEngineRef _executionEngine;

        private ExecutionEngine(CAPI.LLVMExecutionEngineRef executionEngine) => _executionEngine = executionEngine;

        public static ExecutionEngine CreateForModule(Module m)
        {
            if (CAPI.LLVMCreateExecutionEngineForModule(out CAPI.LLVMExecutionEngineRef outEE, m,
                out string? errorMessage))
                throw new InvalidOperationException(errorMessage);
            return new ExecutionEngine(outEE);
        }

        public GenericValue RunFunction(Value f, GenericValue[] args) =>
            new GenericValue(CAPI.LLVMRunFunction(_executionEngine, f,
                Array.ConvertAll(args, a => (CAPI.LLVMGenericValueRef) a)));

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_executionEngine.IsValid) {
                CAPI.LLVMDisposeExecutionEngine(_executionEngine);
                _executionEngine = new CAPI.LLVMExecutionEngineRef();
            }
        }

        ~ExecutionEngine() => DoDispose();

        public Value FindFunction(string name)
        {
            if (!CAPI.LLVMFindFunction(_executionEngine, name, out CAPI.LLVMValueRef outFnRef))
                return outFnRef;
            return new Value();
        }
    }
}
