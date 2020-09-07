namespace Driver.ToolChains
{
    public class MonC : ToolChain
    {
        private static readonly PhaseSet PossiblePhases = ~new PhaseSet(Phase.Backend);

        public override PhaseSet FilterPhases(PhaseSet phases) => phases & PossiblePhases;

        public override ITool BuildCodeGenJobTool(Job job, ICodeGenInput input) =>
            MonCCodeGenTool.Construct(job, this, input);
    }
}