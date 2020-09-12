namespace MonC.SyntaxTree.Leaves.Expressions.BinaryOperations
{
    public class ModuloBinOpLeaf : BinaryOperationLeaf
    {
        public ModuloBinOpLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitModuloBinOp(this);
        }
    }
}
