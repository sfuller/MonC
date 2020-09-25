using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree
{
    public class EnumNode : ITopLevelStatementNode
    {
        public readonly string Name;
        public readonly KeyValuePair<string, int>[] Enumerations;
        public readonly bool IsExported;

        public EnumNode(string name, IEnumerable<KeyValuePair<string, int>> enumerations, bool isExported)
        {
            Name = name;
            Enumerations = enumerations.ToArray();
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
