using System;
using MonC.TypeSystem.Types;

namespace MonC.TypeSystem
{
    public struct TypeGroup
    {
        public IValueType Value;
        public IPointerType? SharedPointer;
        public IPointerType? WeakPointer;
        public IPointerType? OwnedPointer;
        public IPointerType? BorrowedPointer;

        public readonly IType? GetTypeForPointerMode(PointerMode mode)
        {
            return mode switch {
                PointerMode.NotAPointer => Value,
                PointerMode.Shared => SharedPointer,
                PointerMode.Weak => WeakPointer,
                PointerMode.Owned => OwnedPointer,
                PointerMode.Borrowed => BorrowedPointer,
                _ => null
            };
        }

        public void SetTypeForPointerMode(PointerMode mode, IPointerType type)
        {
            switch (mode) {
                case PointerMode.Shared:
                    SharedPointer = type;
                    break;
                case PointerMode.Weak:
                    WeakPointer = type;
                    break;
                case PointerMode.Owned:
                    OwnedPointer = type;
                    break;
                case PointerMode.Borrowed:
                    BorrowedPointer = type;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
