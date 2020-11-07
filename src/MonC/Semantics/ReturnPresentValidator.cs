using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem;

namespace MonC.Semantics
{
    public class ReturnPresentValidator : IStatementVisitor
    {
        private readonly IErrorManager _errors;

        private bool _didReturn;

        public ReturnPresentValidator(IErrorManager errors)
        {
            _errors = errors;
        }

        public void Process(FunctionDefinitionNode function)
        {
            if (TypeUtil.IsVoid(function.ReturnType)) {
                return;
            }

            _didReturn = false;
            VisitBody(function.Body);

            if (!_didReturn) {
                _errors.AddError("Not all code paths return a value", function.Body);
            }
        }

        public void VisitBody(BodyNode node)
        {
            node.VisitStatements(this);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            _didReturn = true;
        }

        public void VisitIfElse(IfElseNode node)
        {
            // TODO: Add unreachable code warning if we've already returned

            if (!_didReturn) {
                node.IfBody.VisitStatements(this);
                if (!_didReturn) {
                    return;
                }
                node.ElseBody.VisitStatements(this);
            }
        }

        public void VisitFor(ForNode node)
        {
            // For body might not run. Do nothing.
        }

        public void VisitWhile(WhileNode node)
        {
            // While body might not run. Do nothing.
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
        }
    }
}
