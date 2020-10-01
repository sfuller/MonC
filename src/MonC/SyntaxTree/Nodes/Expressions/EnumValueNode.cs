namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class EnumValueNode : BasicExpression
    {
        public readonly EnumDeclarationNode Declaration;

        public EnumValueNode(EnumDeclarationNode declaration)
        {
            Declaration = declaration;
        }

        public override void AcceptBasicExpressionVisitor(IBasicExpressionVisitor visitor)
        {
            visitor.VisitEnumValue(this);
        }
    }
}
