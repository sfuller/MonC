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

        public readonly string Name;
        public readonly string ReturnType;
        public readonly Parameter[] Parameters;
        public readonly IASTLeaf Body;

        public FunctionDefinitionLeaf(string name, string returnType, IEnumerable<Parameter> parameters, IASTLeaf body)
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