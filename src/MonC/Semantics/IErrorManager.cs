namespace MonC.Semantics
{
    public interface IErrorManager
    {
        void AddError(string message, ISyntaxTreeNode node);
    }
}
