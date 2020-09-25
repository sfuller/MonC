using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree
{
    public class FunctionDefinitionNode : ITopLevelStatementNode
    {
        public string Name;
        public ITypeSpecifierNode ReturnType;
        public DeclarationNode[] Parameters;
        public BodyNode Body;
        public bool IsExported;

        public FunctionDefinitionNode(
                string name,
                ITypeSpecifierNode returnType,
                IEnumerable<DeclarationNode> parameters,
                BodyNode body,
                bool isExported)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Body = body;
            IsExported = isExported;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitTopLevelStatement(this);
        }

        public void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor)
        {
            visitor.VisitFunctionDefinition(this);
        }

    }
}
