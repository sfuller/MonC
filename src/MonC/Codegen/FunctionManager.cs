using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class FunctionManager
    {
        public readonly Dictionary<string, int> FunctionTable = new Dictionary<string, int>();
        public readonly Dictionary<string, int> DefinedFunctions = new Dictionary<string, int>();
        public readonly Dictionary<string, int> UndefinedFunctions = new Dictionary<string, int>();
        public readonly List<KeyValuePair<string, int>> ExportedFunctions = new List<KeyValuePair<string, int>>();
        
        public void RegisterFunction(FunctionDefinitionLeaf leaf)
        {
            if (DefinedFunctions.ContainsKey(leaf.Name)) {
                return;
            }
            
            int index = FunctionTable.Count;
            DefinedFunctions.Add(leaf.Name, index);
            FunctionTable.Add(leaf.Name, index);
            if (leaf.IsExported) {
                ExportedFunctions.Add(new KeyValuePair<string, int>(leaf.Name, index));    
            }
        }

        public int GetFunctionIndex(FunctionDefinitionLeaf leaf)
        {
            int index;
            if (!FunctionTable.TryGetValue(leaf.Name, out index)) {
                index = FunctionTable.Count;
                UndefinedFunctions.Add(leaf.Name, index);
                FunctionTable.Add(leaf.Name, index);
            }
            return index;
        }
        
    }
}