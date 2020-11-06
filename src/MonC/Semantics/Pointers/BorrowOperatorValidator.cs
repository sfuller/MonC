using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.Semantics.Pointers
{
    /// <summary>
    /// Validates borrow operator rules.
    /// </summary>
    public class BorrowOperatorValidator : IUnaryOperationVisitor
    {
        private readonly IErrorManager _errors;

        public BorrowOperatorValidator(IErrorManager errors)
        {
            _errors = errors;
        }

        public void Process(FunctionDefinitionNode function)
        {
            SyntaxTreeDelegator syntaxTreeDelegator = new SyntaxTreeDelegator();
            ExpressionDelegator expressionDelegator = new ExpressionDelegator();
            expressionDelegator.UnaryOperationVisitor = this;
            syntaxTreeDelegator.ExpressionVisitor = expressionDelegator;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor =
                new StatementChildrenVisitor(syntaxTreeDelegator, childrenVisitor);
            ExpressionChildrenVisitor expressionChildrenVisitor
                = new ExpressionChildrenVisitor(syntaxTreeDelegator, null, childrenVisitor);

            childrenVisitor.StatementVisitor = statementChildrenVisitor;
            childrenVisitor.ExpressionVisitor = expressionChildrenVisitor;

            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void VisitNegateUnaryOp(NegateUnaryOpNode node)
        {
        }

        public void VisitLogicalNotUnaryOp(LogicalNotUnaryOpNode node)
        {
        }

        public void VisitCastUnaryOp(CastUnaryOpNode node)
        {
        }

        public void VisitBorrowUnaryOp(BorrowUnaryOpNode node)
        {
            IAddressableNode? addressableRhs = node.RHS as IAddressableNode;
            if (addressableRhs == null || !addressableRhs.IsAddressable()) {
                _errors.AddError("Cannot borrow non-lvalue type", node);
            }
        }

        public void VisitDereferenceUnaryOp(DereferenceUnaryOpNode node)
        {
        }
    }
}
