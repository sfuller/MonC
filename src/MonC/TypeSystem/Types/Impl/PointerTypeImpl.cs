using System;

namespace MonC.TypeSystem.Types.Impl
{
    public class PointerTypeImpl : IPointerType
    {
        public IValueType DestinationType { get; }
        public PointerMode Mode { get; }

        public PointerTypeImpl(IValueType destinationType, PointerMode mode)
        {
            DestinationType = destinationType;
            Mode = mode;
        }

        public string Represent()
        {
            string pointerSyntax = Mode switch {
                PointerMode.NotAPointer => "",
                PointerMode.Shared => Syntax.POINTER_SHARED,
                PointerMode.Weak => Syntax.POINTER_WEAK,
                PointerMode.Owned => Syntax.POINTER_OWNED,
                PointerMode.Borrowed => Syntax.POINTER_BORROWED,
                _ => throw new NotSupportedException("Unsupported pointer mode")
            };

            return DestinationType.Represent() + pointerSyntax;
        }
    }
}
