namespace MonC.SyntaxTree.Util
{
    public interface IVisitor<T>
    {
        void Visit(T node);
    }
}
