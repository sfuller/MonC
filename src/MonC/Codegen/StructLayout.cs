using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Codegen
{
    public struct MemberLayoutInfo
    {
        public int Index;
        public int Offset;
    }

    public class StructLayout
    {
        public readonly Dictionary<DeclarationNode, MemberLayoutInfo> MemberLayouts;
        public readonly int Size;

        public StructLayout(Dictionary<DeclarationNode, MemberLayoutInfo> memberLayouts, int size)
        {
            MemberLayouts = memberLayouts;
            Size = size;
        }
    }
}
