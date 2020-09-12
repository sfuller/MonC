using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;

namespace MonC.Parsing.ParseTreeLeaves
{
    public class AssignmentParseLeaf : BinaryOperationLeaf, IParseLeaf
    {
        public AssignmentParseLeaf(IExpressionLeaf lhs, IExpressionLeaf rhs) : base(lhs, rhs) { }

        public override void AcceptBinaryOperationVisitor(IBinaryOperationVisitor visitor)
        {
            visitor.VisitUnknown(this);
        }

        public void AcceptParseTreeVisitor(IParseTreeVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }
}
