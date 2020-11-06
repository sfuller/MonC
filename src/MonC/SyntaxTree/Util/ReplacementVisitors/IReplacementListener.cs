namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public interface IReplacementListener
    {
        void NodeReplaced(ISyntaxTreeNode oldNode, ISyntaxTreeNode newNode);
    }
}
