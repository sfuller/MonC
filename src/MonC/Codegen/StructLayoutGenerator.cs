using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class StructLayoutGenerator
    {
        private readonly TypeSizeManager _typeSizeManager;

        public StructLayoutGenerator(TypeSizeManager typeSizeManager)
        {
            _typeSizeManager = typeSizeManager;
        }

        public StructLayout Generate(StructType structType, StructLayoutManager manager)
        {
            Dictionary<DeclarationNode, int> offsets = new Dictionary<DeclarationNode,int>();
            int nextOffset = 0;

            foreach (DeclarationNode declaration in structType.Struct.Members) {
                offsets.Add(declaration, nextOffset);
                nextOffset += _typeSizeManager.GetSize(((TypeSpecifierNode) declaration.Type).Type);
            }

            return new StructLayout(offsets, nextOffset);
        }

    }
}
