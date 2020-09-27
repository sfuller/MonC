namespace MonC.SyntaxTree.Nodes
{
    public class StructFunctionAssociationNode : IStructFunctionAssociationNode
    {
        public string Name;
        public FunctionDefinitionNode FunctionDefinition;

        public StructFunctionAssociationNode(string name, FunctionDefinitionNode functionDefinition)
        {
            Name = name;
            FunctionDefinition = functionDefinition;
        }

        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitStructFunctionAssociation(this);
        }
    }
}
