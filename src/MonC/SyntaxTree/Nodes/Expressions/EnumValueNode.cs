namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class EnumValueNode : BasicExpression
    {
        public readonly EnumNode Enum;
        public readonly string Name;

        public EnumValueNode(EnumNode enumNode, string name)
        {
            Enum = enumNode;
            Name = name;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}
