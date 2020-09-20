using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.TypeSystem;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class TypeSpecifierParseNode : ITypeSpecifierNode, IParseTreeNode
    {
        public string Name;
        public PointerMode PointerMode;

        public TypeSpecifierParseNode(string name, PointerMode pointerMode)
        {
            Name = name;
            PointerMode = pointerMode;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitSpecifier(this);
        }

        public void AcceptSpecifierVisitor(ISpecifierVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitTypeSpecifier(this);
        }
    }
}
