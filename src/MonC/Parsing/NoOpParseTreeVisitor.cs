using MonC.Parsing.ParseTreeLeaves;

namespace MonC.Parsing
{
    public class NoOpParseTreeVisitor : IParseTreeLeafVisitor
    {
        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            
        }
    }
}