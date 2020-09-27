using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class ParseModule
    {
        public readonly List<FunctionDefinitionNode> Functions = new List<FunctionDefinitionNode>();
        public readonly List<EnumNode> Enums = new List<EnumNode>();
        public readonly List<StructNode> Structs = new List<StructNode>();
        public readonly Dictionary<ISyntaxTreeNode, Symbol> SymbolMap = new Dictionary<ISyntaxTreeNode, Symbol>();
    }
}
