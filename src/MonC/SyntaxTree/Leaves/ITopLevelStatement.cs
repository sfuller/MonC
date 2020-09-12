namespace MonC.SyntaxTree
{
    public interface ITopLevelStatement : ISyntaxTreeLeaf
    {
        void AcceptTopLevelVisitor(ITopLevelStatementVisitor visitor);
    }
}