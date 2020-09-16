namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IUnaryOperationNode : IExpressionNode
    {
        public IExpressionNode RHS { get; set; }

        void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor);
    }
}
