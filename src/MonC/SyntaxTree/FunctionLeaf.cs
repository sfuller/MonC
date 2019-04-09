using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class FunctionLeaf : IASTLeaf
    {
        public struct Parameter
        {
            public string Type;
            public string Name;
        }

        public readonly string Name;
        public readonly string ReturnType;
        public readonly Parameter[] Parameters;
        public readonly IASTLeaf[] Statements;

        public FunctionLeaf(string name, string returnType, IEnumerable<Parameter> parameters, IEnumerable<IASTLeaf> statements)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Statements = statements.ToArray();
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitFunction(this);
        }
    }
}