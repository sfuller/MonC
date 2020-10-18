using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public class StructLayout
    {
        public readonly Dictionary<DeclarationNode, int> MemberOffsets;
        public readonly int Size;

        public StructLayout(Dictionary<DeclarationNode, int> memberOffsets, int size)
        {
            MemberOffsets = memberOffsets;
            Size = size;
        }
    }
}
