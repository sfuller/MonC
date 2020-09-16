using System.Collections.Generic;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.Parsing.Semantics
{
    public class TransferPointerAnalysisVisitor : IStatementVisitor, IExpressionVisitor
    {
        private readonly IList<(string message, ISyntaxTreeNode node)> _errors;
        private readonly bool _transferAllowed;
        private readonly DeclarationNode _pointer;

        private bool _ownership = true;

        private TransferPointerAnalysisVisitor MakeSubVisitor(bool transferAllowed = true)
        {
            return new TransferPointerAnalysisVisitor(_pointer, _errors, transferAllowed);
        }

        public TransferPointerAnalysisVisitor(
                DeclarationNode pointer,
                IList<(string message, ISyntaxTreeNode node)> errors,
                bool transferAllowed = true)
        {
            _pointer = pointer;
            _errors = errors;
            _transferAllowed = transferAllowed;
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            // TODO?
        }

        public void VisitFor(ForNode node)
        {
            // TODO?
        }

        public void VisitIfElse(IfElseNode node)
        {
            TransferPointerAnalysisVisitor ifBranchVisitor = MakeSubVisitor();
            TransferPointerAnalysisVisitor elseBranchVisitor = MakeSubVisitor();

            node.Condition.AcceptExpressionVisitor(this);

            node.IfBody.VisitStatements(ifBranchVisitor);
            node.ElseBody.VisitStatements(elseBranchVisitor);

            bool ownershipAfterIfBranch = ifBranchVisitor._ownership;
            bool ownershipAfterElseBranch = elseBranchVisitor._ownership;

            bool hadOwnership = _ownership;
            bool ownershipTaken = !(ownershipAfterIfBranch && ownershipAfterElseBranch);

            if (hadOwnership && ownershipTaken) {
                _ownership = false;

                if (ownershipAfterIfBranch) {
                    // TODO: Add free statement node to If body
                }

                if (ownershipAfterElseBranch) {
                    // TODO: Add free statement node to Else body. This involves creating an else body if null.
                }
            }
        }

        public void VisitWhile(WhileNode node)
        {
            TransferPointerAnalysisVisitor subVisitor = MakeSubVisitor(transferAllowed: false);
            node.Condition.AcceptExpressionVisitor(subVisitor);
            node.Body.VisitStatements(subVisitor);
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            // TODO: Is this sufficient? Are there other types of expressions that could cause ownership transfer?
            if (node.RHS is VariableNode variableNode) {
                if (variableNode.Declaration == _pointer) {
                    TakeOwnership(node);
                }
            }
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            throw new System.NotImplementedException();
        }

        public void VisitVoid(VoidExpressionNode node)
        {
            throw new System.NotImplementedException();
        }

        public void VisitBinaryOperation(IBinaryOperationNode node)
        {
        }

        public void VisitUnaryOperation(IUnaryOperationNode node)
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

        public void VisitFunctionCall(FunctionCallNode node)
        {
            for (int argumentIndex = 0, argumentLength = node.ArgumentCount; argumentIndex < argumentLength; ++argumentIndex) {
                ISyntaxTreeNode argument = node.GetArgument(argumentIndex);
                if (argument == _pointer) {
                    // Note: If same pointer is passed as multiple parameters, an error will occur.
                    // This conventiently prevents the function from possibly accidentally free-ing twice.
                    // We may want to have more formal aliasing errors.
                    TakeOwnership(node);
                }
            }
        }

        public void VisitVariable(VariableNode node)
        {
        }

        public void VisitAssignment(AssignmentNode node)
        {
            // TOOD: What should happen if LHS is the pointer in question? should we allow that pointer to be assigned,
            // and if so, we should free the old value if owned, set ownership = true?

            // TODO: Is this sufficient? Are there other types of expressions that could cause ownership transfer?
            if (node.RHS is VariableNode variableNode) {
                if (variableNode.Declaration == _pointer) {
                    TakeOwnership(node);
                }
            }
        }

        public void VisitUnknown(IExpressionNode node)
        {
        }

        private void TakeOwnership(ISyntaxTreeNode context)
        {
            if (_transferAllowed) {
                _errors.Add(("Cannot transfer ownership in this context.", context));
            }
            if (!_ownership) {
                _errors.Add(("Cannot transfer ownership, previously transferred.", context));
            }
            _ownership = false;
        }
    }
}
