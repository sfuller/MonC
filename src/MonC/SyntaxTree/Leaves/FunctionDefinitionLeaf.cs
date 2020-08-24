using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class FunctionDefinitionLeaf : IASTLeaf
    {
        public struct Parameter
        {
            public TypeSpecifierLeaf Type;
            public string Name;
        }

        public string Name;
        public TypeSpecifierLeaf ReturnType;
        public DeclarationLeaf[] Parameters;
        public BodyLeaf Body;
        public bool IsExported;

        public FunctionDefinitionLeaf(string name, TypeSpecifierLeaf returnType, IEnumerable<DeclarationLeaf> parameters, BodyLeaf body, bool isExported)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Body = body;
            IsExported = isExported;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitFunctionDefinition(this);
        }
    }
}