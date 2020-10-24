namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IAssignableNode : IExpressionNode
    {
        void AcceptAssignableVisitor(IAssignableVisitor visitor);

        /// <summary>
        /// Check if this expression is assignable to. While an IAssignableNode may support assignment, this method
        /// determines if the expression is actually assignable.
        /// </summary>
        bool IsAssignable();
    }
}
