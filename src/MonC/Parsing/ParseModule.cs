using System.Collections.Generic;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class ParseModule
    {
        public readonly List<FunctionDefinitionNode> Functions = new List<FunctionDefinitionNode>();
        public readonly List<EnumNode> Enums = new List<EnumNode>();
        public readonly Dictionary<ISyntaxTreeNode, Symbol> TokenMap = new Dictionary<ISyntaxTreeNode, Symbol>();

        public void RunSemanticAnalysis(ParseModule headerModule, IList<ParseError> errors)
        {
            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors, TokenMap);
            analyzer.Analyze(headerModule, this);
        }
    }
}
