using System;

namespace MonC.LLVM
{
    public class ExecutionEngine
    {
        private CAPI.LLVMExecutionEngineRef _executionEngine;

        private ExecutionEngine(CAPI.LLVMExecutionEngineRef executionEngine) => _executionEngine = executionEngine;

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
    }
}
