using System;

namespace Driver
{
    public static class FileType
    {
        public enum Kind
        {
            UNCLASSIFIED,
            MONC_SOURCE,
            MONC_IL,
            LLVM_ASSEMBLY,
            LLVM_BITCODE,
            TARGET_ASSEMBLY,
            TARGET_OBJECT,
        }

        public static PhaseSet GetPossiblePhases(Kind kind)
        {
            // Target assembly and object artifacts are not supported as input
            // External tools must be used
            switch (kind) {
                default:
                    return new PhaseSet();
                case Kind.MONC_SOURCE:
                    return new PhaseSet(Phase.Lex, Phase.Parse, Phase.CodeGen, Phase.Backend, Phase.Link, Phase.VM);
                case Kind.MONC_IL:
                case Kind.LLVM_ASSEMBLY:
                case Kind.LLVM_BITCODE:
                    return new PhaseSet(Phase.Link, Phase.VM);
            }
        }

        public static Phase GetProducingPhase(Kind kind)
        {
            switch (kind) {
                default:
                    return Phase.Null;
                case Kind.MONC_IL:
                case Kind.LLVM_ASSEMBLY:
                case Kind.LLVM_BITCODE:
                    return Phase.CodeGen;
                case Kind.TARGET_ASSEMBLY:
                case Kind.TARGET_OBJECT:
                    return Phase.Backend;
            }
        }

        public static bool IsCompatibleWithToolchainType(Kind kind, Type toolChainType)
        {
            switch (kind) {
                default:
                    return false;
                case Kind.MONC_SOURCE:
                    return true;
                case Kind.MONC_IL:
                    return toolChainType == typeof(ToolChains.MonC);
                case Kind.LLVM_ASSEMBLY:
                case Kind.LLVM_BITCODE:
                case Kind.TARGET_ASSEMBLY:
                case Kind.TARGET_OBJECT:
                    return toolChainType == typeof(ToolChains.LLVM);
            }
        }
    }
}