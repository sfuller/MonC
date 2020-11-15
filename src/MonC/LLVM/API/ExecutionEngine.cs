using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class ExecutionEngine : IVMModuleArtifact
    {
        private LLVMExecutionEngineRef _executionEngine;

        private ExecutionEngine(LLVMExecutionEngineRef executionEngine) => _executionEngine = executionEngine;

        public static ExecutionEngine CreateForModule(Module m)
        {
            return new ExecutionEngine(((LLVMModuleRef) m).CreateExecutionEngine());
        }

        public GenericValue RunFunction(Value f, GenericValue[] args) =>
            new GenericValue(_executionEngine.RunFunction(f, Array.ConvertAll(args, a => (LLVMGenericValueRef) a)));

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            _executionEngine.Dispose();
        }

        ~ExecutionEngine() => DoDispose();

        public Value FindFunction(string name)
        {
            if (_executionEngine.TryFindFunction(name, out LLVMValueRef outFnRef))
                return outFnRef;
            return new Value();
        }
    }
}
