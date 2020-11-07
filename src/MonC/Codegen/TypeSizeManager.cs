using System;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public interface ITypeSizeManager
    {
        public int GetSize(IType type);
    }

    public class ILTypeSizeManager : ITypeSizeManager
    {
        private readonly StructLayoutManager _structLayoutManager;

        public ILTypeSizeManager(StructLayoutManager structLayoutManager)
        {
            _structLayoutManager = structLayoutManager;
        }

        public int GetSize(IType type)
        {
            return type switch {
                IPrimitiveType primitiveType => GetSize(primitiveType),
                IPointerType pointerType => IntPtr.Size,
                StructType structType => GetSize(structType),
                _ => throw new NotSupportedException()
            };
        }

        private int GetSize(IPrimitiveType type)
        {
            return type.Primitive switch {
                Primitive.Void => 0,
                Primitive.Int => sizeof(int),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private int GetSize(StructType structType)
        {
            return _structLayoutManager.GetLayout(structType).Size;
        }

    }

    public class IndexTypeSizeManager : ITypeSizeManager
    {
        public int GetSize(IType type) => 1;
    }
}
