using System;
using System.Collections.Generic;
using System.IO;
using MonC;
using MonC.DotNetInterop;
using MonC.IL;
using MonC.VM;

namespace Driver.ToolChains
{
    public class MonCLinkTool : IExecutableTool, IVMInput
    {
        private Job _job;
        private ILinkInput _input;

        private MonCLinkTool(Job job, ILinkInput input)
        {
            _job = job;
            _input = input;
        }

        public static MonCLinkTool Construct(Job job, MonC toolchain, ILinkInput input) => new MonCLinkTool(job, input);

        public void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCLinkTool");
        }

        public int Execute()
        {
            // TODO: This should serialize a fully linked VM module to _job._outputFile
            return 0;
        }

        public IVMModuleArtifact GetVMModuleArtifact()
        {
            List<ILModule> modules = _input.GetModuleArtifacts().ConvertAll(m => (ILModule) m);

            List<LinkError> linkErrors = new List<LinkError>();
            Linker linker = new Linker(linkErrors);
            foreach (ILModule module in modules) {
                linker.AddModule(module, export: true);
            }

            foreach (Binding binding in _job.InteropResolver.Bindings) {
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation, export: false);
            }

            VMModule vmModule = linker.Link();

            foreach (LinkError error in linkErrors) {
                Diagnostics.Report(Diagnostics.Severity.Error, $"Link error: {error.Message}");
            }

            Diagnostics.ThrowIfErrors();

            List<string> loadErrors = new List<string>();
            if (!_job.InteropResolver.PrepareForExecution(vmModule, loadErrors)) {
                foreach (string error in loadErrors) {
                    Diagnostics.Report(Diagnostics.Severity.Error, $"Load error: {error}");
                }

                Diagnostics.ThrowIfErrors();
            }

            return vmModule;
        }
    }
}
