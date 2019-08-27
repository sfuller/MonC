namespace MonC.SyntaxTree
{
    public class EnumValueLeaf : IASTLeaf
    {
        public readonly EnumLeaf Enum;
        public readonly string Name;

        public EnumValueLeaf(EnumLeaf enumLeaf, string name)
        {
            Enum = enumLeaf;
            Name = name;
        }

        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}