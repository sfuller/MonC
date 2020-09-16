namespace MonC.SyntaxTree
{
    public interface ITopLevelStatementNode : ISyntaxTreeNode
    {
        void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor);
    }
}
