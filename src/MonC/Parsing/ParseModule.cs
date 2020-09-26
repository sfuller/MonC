using System.Collections.Generic;
using MonC.Semantics;
using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class ParseModule
    {
        public readonly List<FunctionDefinitionNode> Functions = new List<FunctionDefinitionNode>();
        public readonly List<EnumNode> Enums = new List<EnumNode>();
        public readonly Dictionary<ISyntaxTreeNode, Symbol> TokenMap = new Dictionary<ISyntaxTreeNode, Symbol>();

        public bool AddFunction(FunctionDefinitionNode function)
        {
            if (Functions.Exists(n => n.Name == function.Name)) {
                return false;
            }
            Functions.Add(function);
            return true;
        }

        public bool AddEnum(EnumNode enumNode)
        {
            HashSet<string> enumerations = new HashSet<string>();
            foreach (var enumeration in enumNode.Enumerations) {
                enumerations.Add(enumeration.Key);
            }
            foreach (EnumNode existingEnum in Enums) {
                foreach (var existingEnumeration in existingEnum.Enumerations) {
                    if (enumerations.Contains(existingEnumeration.Key)) {
                        return false;
                    }
                }
            }
            Enums.Add(enumNode);
            return true;
        }

        public void RunSemanticAnalysis(ParseModule headerModule, IList<ParseError> errors)
        {
            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors, TokenMap);
            analyzer.Analyze(headerModule, this);
        }
    }
}
