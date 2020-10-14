using System;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class TypeSizeManager
    {
        private readonly StructLayoutManager _structLayoutManager;

        public TypeSizeManager(StructLayoutManager structLayoutManager)
        {
            _structLayoutManager = structLayoutManager;
        }

        public int GetSize(IType type)
        {
            return type switch {
                IPrimitiveType primitiveType => sizeof(int),
                IPointerType pointerType => sizeof(int),
                StructType structType => GetSize(structType),
                _ => throw new NotSupportedException()
            };
        }

        private int GetSize(StructType structType)
        {
            return _structLayoutManager.GetLayout(structType).Size;
        }

    }
}
