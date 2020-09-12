using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class ParseModule
    {
        public readonly List<FunctionDefinitionLeaf> Functions = new List<FunctionDefinitionLeaf>();
        public readonly List<EnumLeaf> Enums = new List<EnumLeaf>();
        public readonly Dictionary<ISyntaxTreeLeaf, Symbol> TokenMap = new Dictionary<ISyntaxTreeLeaf, Symbol>();
    }
}