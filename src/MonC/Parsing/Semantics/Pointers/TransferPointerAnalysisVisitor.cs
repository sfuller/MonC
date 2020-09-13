using System.Collections.Generic;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.Parsing.Semantics
{
    public class TransferPointerAnalysisVisitor : IStatementVisitor, IExpressionVisitor
    {
        private readonly IList<(string message, ISyntaxTreeLeaf leaf)> _errors;
        private readonly bool _transferAllowed;
        private readonly DeclarationLeaf _pointer;

        private bool _ownership = true;

        private TransferPointerAnalysisVisitor MakeSubVisitor(bool transferAllowed = true)
        {
            return new TransferPointerAnalysisVisitor(_pointer, _errors, transferAllowed);
        }

        public TransferPointerAnalysisVisitor(
                DeclarationLeaf pointer,
                IList<(string message, ISyntaxTreeLeaf leaf)> errors,
                bool transferAllowed = true)
        {
            _pointer = pointer;
            _errors = errors;
            _transferAllowed = transferAllowed;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            // TODO?
        }

        public void VisitFor(ForLeaf leaf)
        {
            // TODO?
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            TransferPointerAnalysisVisitor ifBranchVisitor = MakeSubVisitor();
            TransferPointerAnalysisVisitor elseBranchVisitor = MakeSubVisitor();

            leaf.Condition.AcceptExpressionVisitor(this);

            leaf.IfBody.AcceptStatements(ifBranchVisitor);
            leaf.ElseBody.AcceptStatements(elseBranchVisitor);

            bool ownershipAfterIfBranch = ifBranchVisitor._ownership;
            bool ownershipAfterElseBranch = elseBranchVisitor._ownership;

            bool hadOwnership = _ownership;
            bool ownershipTaken = !(ownershipAfterIfBranch && ownershipAfterElseBranch);

            if (hadOwnership && ownershipTaken) {
                _ownership = false;

                if (ownershipAfterIfBranch) {
                    // TODO: Add free statement leaf to If body
                }

                if (ownershipAfterElseBranch) {
                    // TODO: Add free statement leaf to Else body. This involves creating an else body if null.
                }
            }
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            TransferPointerAnalysisVisitor subVisitor = MakeSubVisitor(transferAllowed: false);
            leaf.Condition.AcceptExpressionVisitor(subVisitor);
            leaf.Body.AcceptStatements(subVisitor);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            // TODO: Is this sufficient? Are there other types of expressions that could cause ownership transfer?
            if (leaf.RHS is VariableLeaf variableLeaf) {
                if (variableLeaf.Declaration == _pointer) {
                    TakeOwnership(leaf);
                }
            }
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitVoid(VoidExpression leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitBinaryOperation(IBinaryOperationLeaf leaf)
        {
        }

        public void VisitUnaryOperation(IUnaryOperationLeaf leaf)
        {
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int argumentIndex = 0, argumentLength = leaf.ArgumentCount; argumentIndex < argumentLength; ++argumentIndex) {
                ISyntaxTreeLeaf argument = leaf.GetArgument(argumentIndex);
                if (argument == _pointer) {
                    // Note: If same pointer is passed as multiple parameters, an error will occur.
                    // This conventiently prevents the function from possibly accidentally free-ing twice.
                    // We may want to have more formal aliasing errors.
                    TakeOwnership(leaf);
                }
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            // TOOD: What should happen if LHS is the pointer in question? should we allow that pointer to be assigned,
            // and if so, we should free the old value if owned, set ownership = true?

            // TODO: Is this sufficient? Are there other types of expressions that could cause ownership transfer?
            if (leaf.RHS is VariableLeaf variableLeaf) {
                if (variableLeaf.Declaration == _pointer) {
                    TakeOwnership(leaf);
                }
            }
        }

        public void VisitUnknown(IExpressionLeaf leaf)
        {
        }

        private void TakeOwnership(ISyntaxTreeLeaf context)
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
