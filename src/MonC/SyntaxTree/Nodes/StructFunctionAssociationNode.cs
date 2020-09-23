namespace MonC.SyntaxTree.Nodes
{
    public class StructFunctionAssociationNode : ISyntaxTreeNode
    {
        public void AcceptSyntaxTreeVisitor(ISyntaxTreeVisitor visitor)
        {
            visitor.VisitStructFunctionAssociation(this);
        }
    }
}
