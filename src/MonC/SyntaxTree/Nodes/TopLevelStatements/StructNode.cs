using System.Collections.Generic;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree
{
    public class StructNode : ITopLevelStatementNode
    {
        public string Name;

        public readonly List<IStructFunctionAssociationNode> FunctionAssociations =
                new List<IStructFunctionAssociationNode>();

        public readonly List<DeclarationNode> Members = new List<DeclarationNode>();
        public bool IsExported;

        public StructNode(
                string name,
                IEnumerable<IStructFunctionAssociationNode> functionAssociations,
                IEnumerable<DeclarationNode> declarations,
                bool isExported)
        {
            Name = name;
            FunctionAssociations.AddRange(functionAssociations);
            Members.AddRange(declarations);
            IsExported = isExported;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitTopLevelStatement(this);
        }

        public void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor)
        {
            visitor.VisitStruct(this);
        }
    }
}
