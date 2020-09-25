namespace MonC.SyntaxTree.Nodes.Expressions
{
    public class EnumDeclarationNode : ISyntaxTreeNode
    {
        public string Name;

        public EnumDeclarationNode(string name)
        {
            Name = name;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitEnumDeclaration(this);
        }
    }
}
