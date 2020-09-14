namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class EnumValueLeaf : IExpressionLeaf
    {
        public readonly EnumLeaf Enum;
        public readonly string Name;

        public EnumValueLeaf(EnumLeaf enumLeaf, string name)
        {
            Enum = enumLeaf;
            Name = name;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}
