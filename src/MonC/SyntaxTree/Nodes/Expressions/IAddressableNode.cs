namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IAddressableNode : IExpressionNode
    {
        void AcceptAddressableVisitor(IAddressableVisitor visitor);

        /// <summary>
        /// Check if this expression is addressable. While an IAddressableNode may represent an addressable value, this
        /// method determines if the expression is actually addressable.
        /// </summary>
        bool IsAddressable();
    }
}
