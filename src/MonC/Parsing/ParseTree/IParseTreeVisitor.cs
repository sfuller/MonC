using MonC.Parsing.ParseTreeLeaves;

namespace MonC.Parsing
{
    public interface IParseTreeVisitor
    {
        void VisitAssignment(AssignmentParseLeaf leaf);
        void VisitIdentifier(IdentifierParseLeaf leaf);
        void VisitFunctionCall(FunctionCallParseLeaf leaf);
    }
}
