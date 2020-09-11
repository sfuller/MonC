using System;
using System.IO;

namespace MonC
{
    /// <summary>
    /// Abstract compiler artifact which may potentially own unmanaged resources
    /// </summary>
    public interface IArtifact : IDisposable { }

    /// <summary>
    /// Abstract compiler artifact intended to be used as linker input
    /// </summary>
    public interface IModuleArtifact : IArtifact
    {
        public void WriteListing(TextWriter writer);
    }

    /// <summary>
    /// Abstract compiler artifact intended to be used as VM input
    /// </summary>
    public interface IVMModuleArtifact : IArtifact { }
}
