namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class EnumValueNode : ExpressionNode
    {
        public readonly EnumNode Enum;
        public readonly string Name;

        public EnumValueNode(EnumNode enumNode, string name)
        {
            Enum = enumNode;
            Name = name;
        }

        public override void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}
