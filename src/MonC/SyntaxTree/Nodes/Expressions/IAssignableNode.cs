namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IAssignableNode : IExpressionNode
    {
        void AcceptAssignableVisitor(IAssignableVisitor visitor);
    }
}
