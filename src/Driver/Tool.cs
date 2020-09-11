using System;
using MonC;

namespace Driver
{
    public interface ITool : IInput { }

    public interface IModuleTool : ITool
    {
        public IModuleArtifact GetModuleArtifact();
    }

    public interface IExecutableTool : ITool
    {
        public int Execute();
    }
}
