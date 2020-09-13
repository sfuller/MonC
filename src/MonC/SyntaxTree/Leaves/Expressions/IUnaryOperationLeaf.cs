namespace MonC.SyntaxTree.Leaves.Expressions
{
    public interface IUnaryOperationLeaf : IExpressionLeaf
    {
        public IExpressionLeaf RHS { get; set; }

        void AcceptUnaryOperationVisitor(IUnaryOperationVisitor visitor);
    }
}
