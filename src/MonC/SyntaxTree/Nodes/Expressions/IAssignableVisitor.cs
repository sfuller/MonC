namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IAssignableVisitor
    {
        void VisitVariable(VariableNode node);
        void VisitAccess(AccessNode node);
    }
}
