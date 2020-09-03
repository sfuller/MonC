using System;

namespace MonC.LLVM
{
    public struct Target
    {
        private CAPI.LLVMTargetRef _target;
        public bool IsValid => _target.IsValid;

        private Target(CAPI.LLVMTargetRef target) => _target = target;

        public static Target FromTriple(string triple)
        {
            if (CAPI.LLVMGetTargetFromTriple(triple, out CAPI.LLVMTargetRef target, out string? errorMessage)) {
                if (errorMessage != null)
                    throw new InvalidOperationException(errorMessage);
                throw new InvalidOperationException($"unable to make target of {triple}");
            }

            return new Target(target);
        }

        public static Target FromDefaultTriple() => FromTriple(DefaultTargetTriple);

        public static implicit operator CAPI.LLVMTargetRef(Target target) => target._target;
        public static implicit operator Target(CAPI.LLVMTargetRef target) => new Target(target);

        public TargetMachine CreateTargetMachine(string triple, string cpu, string features,
            CAPI.LLVMCodeGenOptLevel level, CAPI.LLVMRelocMode reloc, CAPI.LLVMCodeModel codeModel)
            => new TargetMachine(CAPI.LLVMCreateTargetMachine(_target, triple, cpu, features, level, reloc, codeModel));

        public static string DefaultTargetTriple => CAPI.LLVMGetDefaultTargetTripleString();

        public static string NormalizeTargetTriple(string triple) => CAPI.LLVMNormalizeTargetTripleString(triple);

        public static string HostCPUName => CAPI.LLVMGetHostCPUNameString();

        public static string HostCPUFeatures => CAPI.LLVMGetHostCPUFeaturesString();

        public static void InitializeAllTargets() => CAPI.LLVMInitializeAllTargets();
    }
}