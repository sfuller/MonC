namespace MonC.TypeSystem.Types
{
    public interface IPrimitiveType : IValueType
    {
        Primitive Primitive { get; }
    }
}
