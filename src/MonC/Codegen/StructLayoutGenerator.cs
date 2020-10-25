using System.Collections.Generic;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class StructLayoutGenerator
    {
        private readonly TypeSizeManager? _typeSizeManager;

        public StructLayoutGenerator(TypeSizeManager? typeSizeManager = null)
        {
            _typeSizeManager = typeSizeManager;
        }

        public StructLayout Generate(StructType structType, StructLayoutManager manager)
        {
            Dictionary<DeclarationNode, MemberLayoutInfo> offsets = new Dictionary<DeclarationNode, MemberLayoutInfo>();
            MemberLayoutInfo nextLayout = new MemberLayoutInfo();

            foreach (DeclarationNode declaration in structType.Struct.Members) {
                offsets.Add(declaration, nextLayout);
                nextLayout.Index += 1;

                // LLVM only cares about member indices and does its own size and offset calculations
                if (_typeSizeManager != null)
                    nextLayout.Offset += _typeSizeManager.GetSize(((TypeSpecifierNode) declaration.Type).Type);
            }

            return new StructLayout(offsets, nextLayout.Offset);
        }
    }
}
