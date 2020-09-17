namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IBinaryOperationNode : IExpressionNode
    {
        IExpressionNode LHS { get; set; }
        IExpressionNode RHS { get; set; }

        void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor);
    }
}
