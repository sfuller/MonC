using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class ParseModule
    {
        public readonly List<FunctionDefinitionLeaf> Functions = new List<FunctionDefinitionLeaf>();
        public readonly List<EnumLeaf> Enums = new List<EnumLeaf>();
        public readonly Dictionary<IASTLeaf, Symbol> TokenMap = new Dictionary<IASTLeaf, Symbol>();
    }
}