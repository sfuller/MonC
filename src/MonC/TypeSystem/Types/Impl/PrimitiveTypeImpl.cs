namespace MonC.TypeSystem.Types.Impl
{
    public class PrimitiveTypeImpl : IPrimitiveType
    {
        public Primitive Primitive { get; }
        public string Name { get; }

        public PrimitiveTypeImpl(Primitive primitive, string name)
        {
            Primitive = primitive;
            Name = name;
        }

        public string Represent()
        {
            return Name;
        }
    }
}
