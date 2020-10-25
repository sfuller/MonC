using System;
using MonC;

namespace Driver
{
    public interface ITool : IInput { }

    public interface IModuleTool : ITool
    {
        public void RunHeaderPass();
        public void RunAnalyserPass();
        public IModuleArtifact GetModuleArtifact();
    }

    public interface IExecutableTool : ITool
    {
        public int Execute();
    }
}
