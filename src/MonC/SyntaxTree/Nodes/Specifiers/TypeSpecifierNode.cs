using MonC.TypeSystem.Types;

namespace MonC.SyntaxTree.Nodes.Specifiers
{
    public class TypeSpecifierNode : ITypeSpecifierNode
    {
        public IType Type;

        public TypeSpecifierNode(IType type)
        {
            Type = type;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitSpecifier(this);
        }

        public void AcceptSpecifierVisitor(ISpecifierVisitor visitor)
        {
            visitor.VisitTypeSpecifier(this);
        }
    }
}
