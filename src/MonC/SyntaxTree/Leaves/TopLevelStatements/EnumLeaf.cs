using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class EnumLeaf : ITopLevelStatement
    {
        public readonly string Name;
        public readonly KeyValuePair<string, int>[] Enumerations;
        public readonly bool IsExported;

        public EnumLeaf(string name, IEnumerable<KeyValuePair<string, int>> enumerations, bool isExported)
        {
            Name = name;
            Enumerations = enumerations.ToArray();
            IsExported = isExported;
        }

        public void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor)
        {
            visitor.VisitEnum(this);
        }

    }
}