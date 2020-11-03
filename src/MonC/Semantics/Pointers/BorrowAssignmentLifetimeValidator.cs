using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Specifiers;
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
    public class BorrowAssignmentLifetimeValidator : IVisitor<ReturnNode>
    {
        private readonly IErrorManager _errors;
        private readonly ScopeManager _scopes;
        private readonly BorrowPointerLifetimeResolver _lifetimeResolver;

        private readonly FunctionDefinitionNode _function;

        public BorrowAssignmentLifetimeValidator(FunctionDefinitionNode function, IErrorManager errors, TypeManager types, ScopeManager scopes)
        {
            _function = function;
            _errors = errors;
            _scopes = scopes;
            _lifetimeResolver = new BorrowPointerLifetimeResolver(types, scopes);
        }

        public void Process()
        {
            SyntaxTreeDelegator syntaxTreeDelegator = new SyntaxTreeDelegator();
            StatementDelegator statementDelegator = new StatementDelegator();
            syntaxTreeDelegator.StatementVisitor = statementDelegator;
            statementDelegator.ReturnVisitor = this;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor =
                new StatementChildrenVisitor(syntaxTreeDelegator, childrenVisitor);
            childrenVisitor.StatementVisitor = statementChildrenVisitor;

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
            if (((TypeSpecifierNode) _function.ReturnType).Type is IPointerType rhsPointerType) {
                if (rhsPointerType.Mode != PointerMode.Borrowed) {
                    return;
                }
            } else {
                return;
            }

            AssertLifetime(_scopes.OuterScope, GetLifetime(node.RHS), node);
        }

        private void AssertLifetime(Scope? lhs, Scope? rhs, ISyntaxTreeNode context)
        {
            if (lhs == null || rhs == null) {
                return;
            }

            if (lhs.Outlives(rhs)) {
                _errors.AddError($"Incompatible lifetime.", context);
            }
        }


    }
}
