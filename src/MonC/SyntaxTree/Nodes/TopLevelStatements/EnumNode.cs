using System.Collections.Generic;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree
{
    public class EnumNode : ITopLevelStatementNode
    {
        public readonly string Name;
        public readonly List<EnumDeclarationNode> Declarations = new List<EnumDeclarationNode>();
        public readonly bool IsExported;

        public EnumNode(string name, IEnumerable<EnumDeclarationNode> declarations, bool isExported)
        {
            Name = name;
            Declarations.AddRange(declarations);
            IsExported = isExported;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitTopLevelStatement(this);
        }

        public void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor)
        {
            visitor.VisitEnum(this);
        }
    }
}
