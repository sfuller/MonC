namespace MonC.TypeSystem.Types
{
    public interface IPointerType : IType
    {
        public IType DestinationType { get; }
        public PointerMode Mode { get; }
    }
}
