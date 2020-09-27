using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class StructFunctionAssociationParseNode : IStructFunctionAssociationNode, IParseTreeNode
    {
        public string Name;
        public string FunctionName;

        public StructFunctionAssociationParseNode(string name, string functionName)
        {
            Name = name;
            FunctionName = functionName;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitStructFunctionAssociation(this);
        }
    }
}
