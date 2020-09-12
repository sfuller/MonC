using System.Collections.Generic;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.Codegen
{
    public class FunctionStackLayout
    {
        public readonly Dictionary<DeclarationLeaf, int> Variables;

        public FunctionStackLayout(IDictionary<DeclarationLeaf, int> variables)
        {
            Variables = new Dictionary<DeclarationLeaf, int>(variables);
        }
    }
}
