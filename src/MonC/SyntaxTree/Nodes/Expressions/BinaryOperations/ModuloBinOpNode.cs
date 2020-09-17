namespace MonC.SyntaxTree.Nodes.Expressions.BinaryOperations
{
    public class ModuloBinOpNode : BinaryOperationNode
    {
        public ModuloBinOpNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitModuloBinOp(this);
        }
    }
}
