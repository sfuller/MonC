namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class AddBinOpLeaf : BinaryOperationLeaf
    {
        public AddBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitAddBinOp(this);
        }
    }
}
