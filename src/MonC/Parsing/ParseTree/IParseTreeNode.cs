namespace MonC.Parsing.ParseTree
{
    public interface IParseTreeNode : ISyntaxTreeNode
    {
        void AcceptParseTreeVisitor(IParseTreeVisitor visitor);
    }
}
