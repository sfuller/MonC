namespace MonC.SyntaxTree.Leaves.Expressions
{
    public interface IBinaryOperationLeaf : IExpressionLeaf
    {
        IExpressionLeaf LHS { get; set; }
        IExpressionLeaf RHS { get; set; }

        void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor);
    }
}
