using MonC.SyntaxTree.Nodes;

namespace MonC
{
    public interface ISyntaxTreeNode
    {
        void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor);
    }
}
