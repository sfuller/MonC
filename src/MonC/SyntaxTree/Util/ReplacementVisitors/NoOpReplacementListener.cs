namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class NoOpReplacementListener : IReplacementListener
    {
        public void NodeReplaced(ISyntaxTreeNode oldNode, ISyntaxTreeNode newNode)
        {
        }

        public static NoOpReplacementListener Instance = new NoOpReplacementListener();
    }
}
