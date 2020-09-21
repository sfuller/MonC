using System.Collections.Generic;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree
{
    public class StructNode : ITopLevelStatementNode
    {
        public string Name;
        public readonly List<DeclarationNode> Members = new List<DeclarationNode>();
        public bool IsExported;

        public StructNode(string name, IEnumerable<DeclarationNode> declarations, bool isExported)
        {
            Name = name;
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
