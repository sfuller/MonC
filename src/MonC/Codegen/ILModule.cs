using System.Collections.Generic;

namespace MonC.Codegen
{
    public class ILModule
    {
        public ILFunction[] DefinedFunctions = new ILFunction[0];
        public string[] UndefinedFunctionNames = new string[0];
        public KeyValuePair<string, int>[] ExportedFunctions = new KeyValuePair<string, int>[0];
        public KeyValuePair<string, int>[] ExportedEnumValues = new KeyValuePair<string, int>[0];
        public string[] Strings = new string[0];
    }
}