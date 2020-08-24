namespace MonC.SyntaxTree
{
    public class TypeSpecifierLeaf : IASTLeaf
    {
        public string Name;
        public PointerType PointerType;

        public TypeSpecifierLeaf()
        {
            Name = "";
        }

        public TypeSpecifierLeaf(string name, PointerType pointerType)
        {
            Name = name;
            PointerType = pointerType;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitTypeSpecifier(this);
        }
    }
}