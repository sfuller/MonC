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
        
        public void RegisterFunction(FunctionDefinitionNode node)
        {
            if (DefinedFunctions.ContainsKey(node.Name)) {
                return;
            }
            
            int index = FunctionTable.Count;
            DefinedFunctions.Add(node.Name, index);
            FunctionTable.Add(node.Name, index);
            if (node.IsExported) {
                ExportedFunctions.Add(new KeyValuePair<string, int>(node.Name, index));    
            }
        }

        public int GetFunctionIndex(FunctionDefinitionNode node)
        {
            int index;
            if (!FunctionTable.TryGetValue(node.Name, out index)) {
                index = FunctionTable.Count;
                UndefinedFunctions.Add(node.Name, index);
                FunctionTable.Add(node.Name, index);
            }
            return index;
        }
        
    }
}