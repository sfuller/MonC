using System.Collections.Generic;
using System.IO;

namespace MonC.Codegen
{
    public sealed class ILModule : ModuleArtifact
    {
        public ILFunction[] DefinedFunctions = new ILFunction[0];
        public string[] UndefinedFunctionNames = new string[0];
        public KeyValuePair<string, int>[] ExportedFunctions = new KeyValuePair<string, int>[0];
        public KeyValuePair<string, int>[] ExportedEnumValues = new KeyValuePair<string, int>[0];
        public string[] Strings = new string[0];

        public override void WriteListing(TextWriter writer)
        {
            ILListingWriter listingWriter = new ILListingWriter(writer);
            listingWriter.Write(this);
        }
    }
}