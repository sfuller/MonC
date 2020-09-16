namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class EnumValueNode : IExpressionNode
    {
        public readonly EnumNode Enum;
        public readonly string Name;

        public EnumValueNode(EnumNode enumNode, string name)
        {
            Enum = enumNode;
            Name = name;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}
