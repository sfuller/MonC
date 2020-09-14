namespace Driver.ToolChains
{
    public class MonC : ToolChain
    {
        private static readonly PhaseSet PossiblePhases = ~new PhaseSet(Phase.Backend);

        public override PhaseSet FilterPhases(PhaseSet phases) => phases & PossiblePhases;

        public override IModuleTool BuildCodeGenJobTool(Job job, ICodeGenInput input) =>
            MonCCodeGenTool.Construct(job, this, input);

        public override IExecutableTool BuildLinkJobTool(Job job, ILinkInput input) =>
            MonCLinkTool.Construct(job, this, input);

        public override IExecutableTool BuildVMJobTool(Job job, IVMInput input) =>
            MonCVMTool.Construct(job, this, input);
    }
}
