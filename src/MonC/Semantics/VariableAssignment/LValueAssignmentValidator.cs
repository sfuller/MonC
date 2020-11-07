using System;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.Semantics
{
    public class LValueAssignmentValidator : IStatementVisitor, IVisitor<VariableNode>, IVisitor<AccessNode>, IVisitor<AssignmentNode>
    {
        private readonly IErrorManager _errors;

        private LValue _targetLValue;
        private bool _isTargetCurrentlyAssigned;

        private readonly LvalueResolver _lValueResolver = new LvalueResolver();
        private readonly SyntaxTreeDelegator _childrenVisitor = new SyntaxTreeDelegator();

        public LValueAssignmentValidator(IErrorManager errors)
        {
            _errors = errors;
            _targetLValue = new LValue(Array.Empty<DeclarationNode>());

            BasicExpressionDelegator basicExpressionDelegator = new BasicExpressionDelegator();
            basicExpressionDelegator.VariableVisitor = this;
            basicExpressionDelegator.AccessVisitor = this;
            basicExpressionDelegator.AssignmentVisitor = this;

            ExpressionDelegator expressionDelegator = new ExpressionDelegator();
            expressionDelegator.BasicVisitor = basicExpressionDelegator;

            SyntaxTreeDelegator visitor = new SyntaxTreeDelegator();
            visitor.ExpressionVisitor = expressionDelegator;
            visitor.StatementVisitor = this;

            _childrenVisitor.ExpressionVisitor = new ExpressionChildrenVisitor(visitor, null, _childrenVisitor);
        }

        public void Validate(LValue targetLValue, FunctionDefinitionNode function)
        {
            _targetLValue = targetLValue;
            _isTargetCurrentlyAssigned = false;

            function.Body.AcceptStatementVisitor(this);
        }

        public void Visit(VariableNode node)
        {
            ValidateUsage(node);
        }

        public void Visit(AccessNode node)
        {
            ValidateUsage(node);
        }

        private void ValidateUsage(IAddressableNode addressableNode)
        {
            LValue? lValue = _lValueResolver.Resolve(addressableNode);
            if (lValue == null) {
                return;
            }

            if (lValue.Covers(_targetLValue) && !_isTargetCurrentlyAssigned) {
                _errors.AddError("Cannot use before assignment", addressableNode);
            }
        }

        public void Visit(AssignmentNode node)
        {
            LValue? assignedLValue = _lValueResolver.Resolve(node.Lhs);
            if (assignedLValue != null && assignedLValue.Covers(_targetLValue)) {
                _isTargetCurrentlyAssigned = true;
            }
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            node.Assignment.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            node.RHS.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }

        public void VisitIfElse(IfElseNode node)
        {
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);
            bool wasAssigned = _isTargetCurrentlyAssigned;

            node.IfBody.AcceptStatementVisitor(this);
            bool assignedInIf = _isTargetCurrentlyAssigned;

            _isTargetCurrentlyAssigned = wasAssigned;
            node.ElseBody.AcceptStatementVisitor(this);
            bool assignedInElse = _isTargetCurrentlyAssigned;

            _isTargetCurrentlyAssigned = wasAssigned || assignedInIf && assignedInElse;
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration.AcceptStatementVisitor(this);
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);

            bool wasAssigned = _isTargetCurrentlyAssigned;
            node.Body.AcceptStatementVisitor(this);
            node.Update.AcceptSyntaxTreeVisitor(_childrenVisitor);
            _isTargetCurrentlyAssigned = wasAssigned;
        }

        public void VisitWhile(WhileNode node)
        {
            node.Condition.AcceptSyntaxTreeVisitor(_childrenVisitor);

            bool wasAssigned = _isTargetCurrentlyAssigned;
            node.Body.AcceptStatementVisitor(this);
            _isTargetCurrentlyAssigned = wasAssigned;
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }
    }
}
