using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;

namespace MonC.Semantics.Structs
{
    public class FunctionAssociationAnalyzer
    {
        public void Process(StructNode structNode, SemanticContext context, IErrorManager errors)
        {
            for (int i = 0, ilen = structNode.FunctionAssociations.Count; i < ilen; ++i) {
                IStructFunctionAssociationNode associationNode = structNode.FunctionAssociations[i];

                if (associationNode is StructFunctionAssociationParseNode parseNode) {
                    if (!context.Functions.TryGetValue(parseNode.FunctionName, out FunctionDefinitionNode function)) {
                        errors.AddError($"Undefined function \"{parseNode.FunctionName}\"", parseNode);
                        continue;
                    }

                    structNode.FunctionAssociations[i] = new StructFunctionAssociationNode(parseNode.Name, function);
                }
            }
        }

    }
}
