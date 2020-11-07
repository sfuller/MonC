using MonC.Semantics.Scoping;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics.Pointers
{
    public class BorrowPointerLifetimeResolver : IExpressionVisitor, IBasicExpressionVisitor
    {
        private ScopeManager _scopeManager;

        public Scope? Lifetime { get; private set; }

        public BorrowPointerLifetimeResolver(ScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
        }

        public void Reset()
        {
            Lifetime = null;
        }

        public void VisitBasicExpression(IBasicExpression node)
        {
            node.AcceptBasicExpressionVisitor(this);
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
        {
            if (node is BorrowUnaryOpNode) {
                node.RHS.AcceptExpressionVisitor(this);
            }
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
        }

        public void VisitUnknown(IExpressionNode node)
        {
        }

        public void VisitVoid(VoidExpressionNode node)
        {
        }

        public void VisitNumericLiteral(NumericLiteralNode node)
        {
        }

        public void VisitStringLiteral(StringLiteralNode node)
        {
        }

        public void VisitEnumValue(EnumValueNode node)
        {
        }

        public void VisitVariable(VariableNode node)
        {
            DetermineLifetimeFromDeclaration(node.Declaration);
        }

        private bool IsBorrowPointer(ITypeSpecifierNode node)
        {
            if (!(node is TypeSpecifierNode typeSpecifier)) {
                return false;
            }

            if (!(typeSpecifier.Type is IPointerType pointerType)) {
                return false;
            }

            if (pointerType.Mode != PointerMode.Borrowed) {
                return false;
            }

            return true;
        }

        private void DetermineLifetimeFromDeclaration(DeclarationNode declarationNode)
        {
            Lifetime = _scopeManager.GetScope(declarationNode).Scope;
        }

        public void VisitFunctionCall(FunctionCallNode node)
        {
            if (!IsBorrowPointer(node.LHS.ReturnType)) {
                return;
            }

            Scope tightestLifetime = _scopeManager.OuterScope;

            foreach (IExpressionNode argument in node.Arguments) {
                Lifetime = null;
                argument.AcceptExpressionVisitor(this);
                if (Lifetime != null && tightestLifetime.Outlives(Lifetime)) {
                    tightestLifetime = Lifetime;
                }
            }

            Lifetime = tightestLifetime;
        }

        public void VisitAssignment(AssignmentNode node)
        {
            node.Rhs.AcceptExpressionVisitor(this);
        }

        public void VisitAccess(AccessNode node)
        {
            node.Lhs.AcceptExpressionVisitor(this);
        }
    }
}
