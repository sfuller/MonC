using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class TargetMachine : IDisposable
    {
        private LLVMTargetMachineRef _targetMachine;

        public static implicit operator LLVMTargetMachineRef(TargetMachine targetMachine) =>
            targetMachine._targetMachine;

        internal TargetMachine(LLVMTargetMachineRef targetMachine) => _targetMachine = targetMachine;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private unsafe void DoDispose()
        {
            if (_targetMachine.Handle != IntPtr.Zero) {
                LLVMSharp.Interop.LLVM.DisposeTargetMachine(_targetMachine);
                _targetMachine = new LLVMTargetMachineRef();
            }
        }

        ~TargetMachine() => DoDispose();

        public unsafe Target TargetMachineTarget =>
            (LLVMTargetRef) LLVMSharp.Interop.LLVM.GetTargetMachineTarget(_targetMachine);

        public unsafe string TargetMachineTriple => MarshaledString.NativeToManagedDispose(
            LLVMSharp.Interop.LLVM.GetTargetMachineTriple(_targetMachine));

        public unsafe string TargetMachineCPU => MarshaledString.NativeToManagedDispose(
            LLVMSharp.Interop.LLVM.GetTargetMachineCPU(_targetMachine));

        public unsafe string TargetMachineFeatureString => MarshaledString.NativeToManagedDispose(
            LLVMSharp.Interop.LLVM.GetTargetMachineFeatureString(_targetMachine));

        public unsafe void SetAsmVerbosity(bool verboseAsm) =>
            LLVMSharp.Interop.LLVM.SetTargetMachineAsmVerbosity(_targetMachine, verboseAsm ? 1 : 0);

        public void EmitToFile(Module m, string filename, LLVMCodeGenFileType codegen) =>
            _targetMachine.EmitToFile(m, filename, codegen);

        public unsafe MemoryBuffer EmitToMemoryBuffer(Module m, LLVMCodeGenFileType codegen)
        {
            sbyte* errMsg = null;
            LLVMMemoryBufferRef memBuf;
            if (LLVMSharp.Interop.LLVM.TargetMachineEmitToMemoryBuffer(_targetMachine, (LLVMModuleRef) m, codegen,
                &errMsg, (LLVMOpaqueMemoryBuffer**) &memBuf) != 0) {
                if (errMsg != null)
                    throw new InvalidOperationException(MarshaledString.NativeToManagedDispose(errMsg));
                throw new InvalidOperationException($"unable to emit to memory buffer");
            }

            return new MemoryBuffer(memBuf);
        }

        public unsafe TargetData CreateTargetDataLayout() =>
            new TargetData(LLVMSharp.Interop.LLVM.CreateTargetDataLayout(_targetMachine));
    }
}
