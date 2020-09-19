namespace MonC.TypeSystem.Types.Impl
{
    public class PointerTypeImpl : IPointerType
    {
        public IType DestinationType { get; }
        public PointerMode Mode { get; }

        public PointerTypeImpl(IType destinationType, PointerMode mode)
        {
            DestinationType = destinationType;
            Mode = mode;
        }
    }
}
