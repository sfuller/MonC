namespace MonC.SyntaxTree.Nodes
{
    public interface ISpecifierNode : ISyntaxTreeNode
    {
        public void AcceptSpecifierVisitor(ISpecifierVisitor visitor);
    }
}
