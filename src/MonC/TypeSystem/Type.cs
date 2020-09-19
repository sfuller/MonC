namespace MonC.TypeSystem
{
    public class Type
    {
        public readonly Type? InnerType;
        public readonly string Name;
        public readonly PointerMode PointerMode;

        public Type(string name)
        {
            Name = name;
        }

        public Type(Type innerType, PointerMode pointerMode)
        {
            Name = innerType.Name;
            InnerType = innerType;
            PointerMode = pointerMode;
        }

    }
}
