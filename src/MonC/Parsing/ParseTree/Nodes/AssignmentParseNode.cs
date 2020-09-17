using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.Parsing.ParseTree.Nodes
{
    public class AssignmentParseNode : BinaryOperationNode, IParseTreeNode
    {
        public AssignmentParseNode(IExpressionNode lhs, IExpressionNode rhs) : base(lhs, rhs) { }

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
