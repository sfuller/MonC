using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class BasicExpressionDelegator : IBasicExpressionVisitor
    {
        public IVisitor<VoidExpressionNode>? VoidExpressionVisitor;
        public IVisitor<NumericLiteralNode>? NumericLiteralVisitor;
        public IVisitor<StringLiteralNode>? StringLiteralVisitor;
        public IVisitor<EnumValueNode>? EnumValueVisitor;
        public IVisitor<VariableNode>? VariableVisitor;
        public IVisitor<FunctionCallNode>? FunctionCallVisitor;
        public IVisitor<AssignmentNode>? AssignmentVisitor;
        public IVisitor<AccessNode>? AccessVisitor;

        public void VisitVoid(VoidExpressionNode node)
        {
            VoidExpressionVisitor?.Visit(node);
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
            NumericLiteralVisitor?.Visit(node);
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
            StringLiteralVisitor?.Visit(node);
        }

        public void VisitEnumValue(EnumValueNode node)
        {
            EnumValueVisitor?.Visit(node);
        }

        public void VisitVariable(VariableNode node)
        {
            VariableVisitor?.Visit(node);
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            FunctionCallVisitor?.Visit(node);
        }

        public void VisitAssignment(AssignmentNode node)
        {
            AssignmentVisitor?.Visit(node);
        }

        public void VisitAccess(AccessNode node)
        {
            AccessVisitor?.Visit(node);
        }
    }
}
