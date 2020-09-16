using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree
{
    public class FunctionDefinitionLeaf : ITopLevelStatement
    {
        public string Name;
        public TypeSpecifier ReturnType;
        public DeclarationLeaf[] Parameters;
        public BodyLeaf Body;
        public bool IsExported;

        public FunctionDefinitionLeaf(
                string name,
                TypeSpecifier returnType,
                IEnumerable<DeclarationLeaf> parameters,
                BodyLeaf body,
                bool isExported)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Body = body;
            IsExported = isExported;
        }

        public void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor)
        {
            visitor.VisitFunctionDefinition(this);
        }

    }
}
