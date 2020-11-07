namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IAddressableVisitor
    {
        void VisitVariable(VariableNode node);
        void VisitAccess(AccessNode node);
    }
}
