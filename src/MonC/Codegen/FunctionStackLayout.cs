using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class FunctionStackLayout
    {
        public readonly Dictionary<DeclarationNode, int> Variables;
        public readonly int ReturnValueSize;
        public readonly int ArgumentsSize;
        public readonly int EndAddress;

        public FunctionStackLayout(IDictionary<DeclarationNode, int> variables, int returnValueSize, int argumentsSize, int endAddress)
        {
            Variables = new Dictionary<DeclarationNode, int>(variables);
            ReturnValueSize = returnValueSize;
            ArgumentsSize = argumentsSize;
            EndAddress = endAddress;
        }
    }
}
