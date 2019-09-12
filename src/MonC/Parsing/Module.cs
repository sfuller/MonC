using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class Module
    {
        public readonly List<FunctionDefinitionLeaf> Functions = new List<FunctionDefinitionLeaf>();
        public readonly List<EnumLeaf> Enums = new List<EnumLeaf>();
        public readonly Dictionary<IASTLeaf, TokenRange> TokenMap = new Dictionary<IASTLeaf, TokenRange>();
    }
}