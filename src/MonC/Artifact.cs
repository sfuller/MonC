using System;
using System.IO;

namespace MonC
{
    /// <summary>
    /// Abstract compiler artifact which may potentially own unmanaged resources
    /// </summary>
    public abstract class Artifact : IDisposable
    {
        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// Abstract compiler artifact intended to be used as linker input
    /// </summary>
    public abstract class ModuleArtifact : Artifact
    {
        public virtual void WriteListing(TextWriter writer)
        {
        }
    }

    /// <summary>
    /// Abstract compiler artifact intended to be used as VM input
    /// </summary>
    public abstract class ExecutableArtifact : Artifact
    {
    }
}