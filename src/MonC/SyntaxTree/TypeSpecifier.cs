namespace MonC.SyntaxTree
{
    public class TypeSpecifier
    {
        public string Name;
        public PointerType PointerType;

        public TypeSpecifier()
        {
            Name = "";
        }

        public TypeSpecifier(string name, PointerType pointerType)
        {
            Name = name;
            PointerType = pointerType;
        }
    }
}