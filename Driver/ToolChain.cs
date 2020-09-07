using System;
using System.Collections.Generic;
using System.IO;

namespace Driver
{
    public abstract class ToolChain
    {
        public static readonly KeyValuePair<string, Type>[] ToolChains =
        {
            new KeyValuePair<string, Type>("monc", typeof(ToolChains.MonC)),
            new KeyValuePair<string, Type>("llvm", typeof(ToolChains.LLVM))
        };

        public static bool TryGetToolchain(string key, out Type value)
        {
            foreach (var pair in ToolChains) {
                if (pair.Key == key) {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public static void WriteOutToolChains(TextWriter writer)
        {
            foreach (var toolChain in ToolChains) {
                writer.WriteLine($"  {toolChain.Key}");
            }
        }

        public virtual Phase SelectRelocTargetPhase(Phase outputFilePhase) => Phase.CodeGen;

        public virtual PhaseSet FilterPhases(PhaseSet phases) => phases;

        public ITool BuildLexJobTool(Job job, ILexInput input) => LexTool.Construct(job, input);

        public ITool BuildParseJobTool(Job job, IParseInput input) => ParseTool.Construct(job, input);

        public virtual ITool BuildCodeGenJobTool(Job job, ICodeGenInput input) => throw new NotImplementedException();

        public virtual ITool BuildBackendJobTool(Job job, IBackendInput input) => throw new NotImplementedException();

        public virtual ITool BuildLinkJobTool(Job job, ILinkInput input) => throw new NotImplementedException();

        public virtual ITool BuildVMJobTool(Job job, IVMInput input) => throw new NotImplementedException();
    }
}