namespace MonC.Parsing.ParseTree
{
    public interface IParseTreeNode
    {
        void AcceptParseTreeVisitor(IParseTreeVisitor visitor);
    }
}
