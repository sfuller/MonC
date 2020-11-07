namespace MonC.TypeSystem.Types
{
    public interface IPointerType : IType
    {
        public IValueType DestinationType { get; }
        public PointerMode Mode { get; }
    }
}
