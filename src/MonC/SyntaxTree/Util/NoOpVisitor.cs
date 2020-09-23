namespace MonC.SyntaxTree.Util
{
    public class NoOpVisitor<T> : IVisitor<T>
    {
        public void Visit(T node) { }
    }
}
