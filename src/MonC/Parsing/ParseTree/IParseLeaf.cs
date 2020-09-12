namespace MonC.Parsing
{
    public interface IParseLeaf
    {
        void AcceptParseTreeVisitor(IParseTreeVisitor visitor);
    }
}
