using System;

namespace MonC.LLVM
{
    public class TargetMachine : IDisposable
    {
        private CAPI.LLVMTargetMachineRef _targetMachine;

        public static implicit operator CAPI.LLVMTargetMachineRef(TargetMachine targetMachine) =>
            targetMachine._targetMachine;

        internal TargetMachine(CAPI.LLVMTargetMachineRef targetMachine) => _targetMachine = targetMachine;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_targetMachine.IsValid) {
                CAPI.LLVMDisposeTargetMachine(_targetMachine);
                _targetMachine = new CAPI.LLVMTargetMachineRef();
            }
        }

        ~TargetMachine() => DoDispose();

        public Target TargetMachineTarget => CAPI.LLVMGetTargetMachineTarget(_targetMachine);

        public string TargetMachineTriple => CAPI.LLVMGetTargetMachineTriple(_targetMachine);

        public string TargetMachineCPU => CAPI.LLVMGetTargetMachineCPU(_targetMachine);

        public string TargetMachineFeatureString => CAPI.LLVMGetTargetMachineFeatureString(_targetMachine);

        public void SetAsmVerbosity(bool verboseAsm) =>
            CAPI.LLVMSetTargetMachineAsmVerbosity(_targetMachine, verboseAsm);

        public void EmitToFile(Module m, string filename, CAPI.LLVMCodeGenFileType codegen)
        {
            if (CAPI.LLVMTargetMachineEmitToFile(_targetMachine, m, filename, codegen, out string? errorMessage)) {
                if (errorMessage != null)
                    throw new InvalidOperationException(errorMessage);
                throw new InvalidOperationException($"unable to emit to {filename}");
            }
        }

        public MemoryBuffer EmitToMemoryBuffer(Module m, string filename, CAPI.LLVMCodeGenFileType codegen)
        {
            if (CAPI.LLVMTargetMachineEmitToMemoryBuffer(_targetMachine, m, codegen, out string? errorMessage,
                out CAPI.LLVMMemoryBufferRef outMemBuf)) {
                if (errorMessage != null)
                    throw new InvalidOperationException(errorMessage);
                throw new InvalidOperationException($"unable to emit to {filename}");
            }

            return new MemoryBuffer(outMemBuf);
        }
    }
}