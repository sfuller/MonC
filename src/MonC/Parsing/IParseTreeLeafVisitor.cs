using MonC.Parsing.ParseTreeLeaves;

namespace MonC.Parsing
{
    public interface IParseTreeLeafVisitor
    {
        void VisitIdentifier(IdentifierParseLeaf leaf);
        void VisitFunctionCall(FunctionCallParseLeaf leaf);
    }
}