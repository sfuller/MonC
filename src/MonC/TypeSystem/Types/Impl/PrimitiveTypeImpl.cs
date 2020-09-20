namespace MonC.TypeSystem.Types.Impl
{
    public class PrimitiveTypeImpl : IPrimitiveType
    {
        public string Name { get; }

        public PrimitiveTypeImpl(string name)
        {
            Name = name;
        }

        public string Represent()
        {
            return Name;
        }
    }
}
