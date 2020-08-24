using System.Collections.Generic;
using System.Linq;

namespace MonC.SyntaxTree
{
    public class EnumLeaf : IASTLeaf
    {
        public readonly KeyValuePair<string, int>[] Enumerations;
        public readonly bool IsExported;

        public EnumLeaf(IEnumerable<KeyValuePair<string, int>> enumerations, bool isExported)
        {
            Enumerations = enumerations.ToArray();
            IsExported = isExported;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitEnum(this);
        }
    }
}