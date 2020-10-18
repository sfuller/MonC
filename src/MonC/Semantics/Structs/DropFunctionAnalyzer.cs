using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;

namespace MonC.Semantics.Structs
{
    public class DropFunctionAnalyzer
    {
        public void Analyze(StructNode node, IErrorManager errors)
        {
            foreach (IStructFunctionAssociationNode associationNode in node.FunctionAssociations) {
                if (!(associationNode is StructFunctionAssociationNode processedAssociationNode)) {
                    continue;
                }

                if (processedAssociationNode.Name == Syntax.FUNCTION_ATTRIBUTE_DROP
                    && !processedAssociationNode.FunctionDefinition.IsDrop) {
                    errors.AddError(
                        "Associated drop function is not defined with drop attribute.",
                        processedAssociationNode);
                }
            }
        }
    }
}
