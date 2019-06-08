using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class FunctionDefinitionLeaf : IASTLeaf
    {
        public struct Parameter
        {
            public string Type;
            public string Name;
        }

        public string Name;
        public string ReturnType;
        public DeclarationLeaf[] Parameters;
        public IASTLeaf Body;

        public FunctionDefinitionLeaf(string name, string returnType, IEnumerable<DeclarationLeaf> parameters, IASTLeaf body)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Body = body;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitFunctionDefinition(this);
        }
    }
}