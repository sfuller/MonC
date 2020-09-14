using System.Collections.Generic;
using System.IO;

namespace MonC.IL
{
    public sealed class ILModule : IModuleArtifact
    {
        public ILFunction[] DefinedFunctions = new ILFunction[0];
        public string[] UndefinedFunctionNames = new string[0];
        public KeyValuePair<string, int>[] ExportedFunctions = new KeyValuePair<string, int>[0];
        public KeyValuePair<string, int>[] ExportedEnumValues = new KeyValuePair<string, int>[0];
        public string[] Strings = new string[0];

        public void WriteListing(TextWriter writer)
        {
            ILListingWriter listingWriter = new ILListingWriter(writer);
            listingWriter.Write(this);
        }

        public void Dispose() { }
    }
}
