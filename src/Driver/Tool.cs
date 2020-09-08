using System;
using MonC;

namespace Driver
{
    public class Artifact : IDisposable
    {
        public void Dispose()
        {
        }
    }
    
    public interface ITool : IInput
    {
    }

    public interface IModuleTool : ITool
    {
        public ModuleArtifact GetModuleArtifact();
    }

    public interface IExecutableTool : ITool
    {
        public void Execute();
    }
}