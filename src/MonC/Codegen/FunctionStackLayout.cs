using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class FunctionStackLayout
    {
        public readonly Dictionary<DeclarationNode, int> Variables;

        public FunctionStackLayout(IDictionary<DeclarationNode, int> variables)
        {
            Variables = new Dictionary<DeclarationNode, int>(variables);
        }
    }
}
