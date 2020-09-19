using MonC.SyntaxTree.Nodes.Specifiers;

namespace MonC.SyntaxTree.Nodes
{
    public interface ISpecifierVisitor
    {
        public void VisitTypeSpecifier(TypeSpecifierNode node);

        void VisitUnknown(ISpecifierNode node);
    }
}
