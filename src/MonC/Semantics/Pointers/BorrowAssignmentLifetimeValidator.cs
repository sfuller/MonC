using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics.Pointers
{
    /// <summary>
    /// Validates that borrow pointers being assigned to borrow pointer l-values have compatible lifetimes.
    /// </summary>
    public class BorrowAssignmentLifetimeValidator : IVisitor<ReturnNode>, IVisitor<AssignmentNode>
    {
        private readonly IErrorManager _errors;
        private readonly ScopeManager _scopes;
        private readonly ExpressionTypeManager _types;
        private readonly BorrowPointerLifetimeResolver _lifetimeResolver;

        private readonly FunctionDefinitionNode _function;

        public BorrowAssignmentLifetimeValidator(FunctionDefinitionNode function, IErrorManager errors, ExpressionTypeManager types, ScopeManager scopes)
        {
            _function = function;
            _errors = errors;
            _scopes = scopes;
            _types = types;
            _lifetimeResolver = new BorrowPointerLifetimeResolver(scopes);
        }

        public void Process()
        {
            SyntaxTreeDelegator syntaxTreeDelegator = new SyntaxTreeDelegator();
            StatementDelegator statementDelegator = new StatementDelegator();
            ExpressionDelegator expressionDelegator = new ExpressionDelegator();
            BasicExpressionDelegator basicExpressionDelegator = new BasicExpressionDelegator();
            syntaxTreeDelegator.StatementVisitor = statementDelegator;
            statementDelegator.ReturnVisitor = this;
            syntaxTreeDelegator.ExpressionVisitor = expressionDelegator;
            expressionDelegator.BasicVisitor = basicExpressionDelegator;
            basicExpressionDelegator.AssignmentVisitor = this;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor =
                new StatementChildrenVisitor(syntaxTreeDelegator, childrenVisitor);
            childrenVisitor.StatementVisitor = statementChildrenVisitor;
            childrenVisitor.ExpressionVisitor = new ExpressionChildrenVisitor(syntaxTreeDelegator, null, childrenVisitor);

            _function.Body.VisitStatements(statementChildrenVisitor);
        }

        private Scope? GetLifetime(IExpressionNode node)
        {
            _lifetimeResolver.Reset();
            node.AcceptExpressionVisitor(_lifetimeResolver);
            return _lifetimeResolver.Lifetime;
        }

        public void Visit(ReturnNode node)
        {
            AssertLifetime(TypeUtil.GetTypeFromSpecifier(_function.ReturnType), _scopes.OuterScope, GetLifetime(node.RHS), node);
        }

        public void Visit(AssignmentNode node)
        {
            AssertLifetime(_types.GetExpressionType(node.Lhs), GetLifetime(node.Lhs), GetLifetime(node.Rhs), node);
        }

        private void AssertLifetime(IType? lhsType, Scope? lhs, Scope? rhs, ISyntaxTreeNode context)
        {
            if (lhsType is IPointerType rhsPointerType) {
                if (rhsPointerType.Mode != PointerMode.Borrowed) {
                    return;
                }
            } else {
                return;
            }

            if (lhs == null || rhs == null) {
                return;
            }

            if (lhs.Outlives(rhs)) {
                _errors.AddError("Incompatible lifetime.", context);
            }
        }
    }
}
