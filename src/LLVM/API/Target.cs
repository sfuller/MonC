using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public struct Target
    {
        private LLVMTargetRef _target;
        public bool IsValid => _target.Handle != IntPtr.Zero;

        private Target(LLVMTargetRef target) => _target = target;

        public static unsafe Target FromTriple(string triple)
        {
            using var marshaledTriple = new MarshaledString(triple.AsSpan());
            LLVMTargetRef target;
            sbyte* errMsg = null;
            if (LLVMSharp.Interop.LLVM.GetTargetFromTriple(marshaledTriple, (LLVMTarget**) &target, &errMsg) != 0) {
                if (errMsg != null)
                    throw new InvalidOperationException(MarshaledString.NativeToManagedDispose(errMsg));
                throw new InvalidOperationException($"unable to make target of {triple}");
            }

            return new Target(target);
        }

        public static Target FromDefaultTriple() => FromTriple(DefaultTargetTriple);

        public static implicit operator LLVMTargetRef(Target target) => target._target;
        public static implicit operator Target(LLVMTargetRef target) => new Target(target);

        public TargetMachine CreateTargetMachine(string triple, string cpu, string features, LLVMCodeGenOptLevel level,
            LLVMRelocMode reloc, LLVMCodeModel codeModel) =>
            new TargetMachine(_target.CreateTargetMachine(triple, cpu, features, level, reloc, codeModel));

        public static unsafe string DefaultTargetTriple =>
            MarshaledString.NativeToManagedDispose(LLVMSharp.Interop.LLVM.GetDefaultTargetTriple());

        public static unsafe string NormalizeTargetTriple(string triple)
        {
            using var marshaledTriple = new MarshaledString(triple.AsSpan());
            return MarshaledString.NativeToManagedDispose(
                LLVMSharp.Interop.LLVM.NormalizeTargetTriple(marshaledTriple));
        }

        public static unsafe string HostCPUName =>
            MarshaledString.NativeToManagedDispose(LLVMSharp.Interop.LLVM.GetHostCPUName());

        public static unsafe string HostCPUFeatures =>
            MarshaledString.NativeToManagedDispose(LLVMSharp.Interop.LLVM.GetHostCPUFeatures());

        public static void InitializeAllTargets()
        {
            LLVMSharp.Interop.LLVM.InitializeAllTargetInfos();
            LLVMSharp.Interop.LLVM.InitializeAllTargets();
            LLVMSharp.Interop.LLVM.InitializeAllTargetMCs();
            LLVMSharp.Interop.LLVM.InitializeAllAsmPrinters();
        }
    }
}
