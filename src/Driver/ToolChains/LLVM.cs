namespace Driver.ToolChains
{
    [CommandLineCategory]
    public class LLVM : ToolChain
    {
        [CommandLine("-g", "Include debug information in module")]
        private bool _debugInfo = false;

        [CommandLine("-gcolumn-info", "Include column debug information in module")]
        private bool _debugColumnInfo = false;

        [CommandLine("-native", "Output native relocatable, assembly, or linked executable")]
        private bool _native = false;

        [CommandLine("-target", "Specify output target triple", "target")]
        private string _target = null;

        [CommandLine("-O0", "Optimization level 0")]
        private bool _O0 = false;

        [CommandLine("-O1", "Optimization level 1")]
        private bool _O1 = false;

        [CommandLine("-O2", "Optimization level 2")]
        private bool _O2 = false;

        [CommandLine("-Os", "Optimization level 2 minimum size priority")]
        private bool _Os = false;

        [CommandLine("-Oz", "Optimization level 2 extra minimum size priority")]
        private bool _Oz = false;

        [CommandLine("-O3", "Optimization level 3")]
        private bool _O3 = false;

        public override Phase SelectRelocTargetPhase(Phase outputFilePhase)
        {
            if (_native)
                return Phase.Backend;

            if (outputFilePhase == Phase.Backend) {
                // Implied native if output file extension calls for it
                _native = true;
                return Phase.Backend;
            }
            
            return Phase.CodeGen;
        }

        public override ITool BuildCodeGenJobTool(Job job, ICodeGenInput input) =>
            LLVMCodeGenTool.Construct(job, this, input);

        public override ITool BuildBackendJobTool(Job job, IBackendInput input) =>
            throw new System.NotImplementedException();
    }
}