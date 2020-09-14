using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessStatementReplacementsVisitor : IStatementVisitor
    {
        public readonly IStatementReplacementVisitor _statementReplacer;
        public readonly IExpressionReplacementVisitor _expressionReplacer;

        public ProcessStatementReplacementsVisitor(IStatementReplacementVisitor statementReplacer, IExpressionReplacementVisitor expressionReplacer)
        {
            _statementReplacer = statementReplacer;
            _expressionReplacer = expressionReplacer;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            leaf.Assignment = ProcessExpressionReplacement(leaf.Assignment);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            leaf.RHS = ProcessExpressionReplacement(leaf.RHS);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition = ProcessExpressionReplacement(leaf.Condition);
            ProcessBodyReplacements(leaf.IfBody);
            ProcessBodyReplacements(leaf.ElseBody);
        }

        public void VisitFor(ForLeaf leaf)
        {
            // TODO: Eliminate type cast -- specific visitors for each type of replacement needed.
            leaf.Declaration = (DeclarationLeaf) ProcessStatementReplacement(leaf.Declaration);
            leaf.Condition = ProcessExpressionReplacement(leaf.Condition);
            leaf.Update = ProcessExpressionReplacement(leaf.Update);
            ProcessBodyReplacements(leaf.Body);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            leaf.Condition = ProcessExpressionReplacement(leaf.Condition);
            ProcessBodyReplacements(leaf.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            leaf.Expression = ProcessExpressionReplacement(leaf.Expression);
        }

        private void ProcessBodyReplacements(Body body)
        {
            for (int i = 0, ilen = body.Length; i < ilen; ++i) {
                IStatementLeaf statement = body.GetStatement(i);
                body.SetStatement(i, ProcessStatementReplacement(statement));
            }
        }

        private IExpressionLeaf ProcessExpressionReplacement(IExpressionLeaf leaf)
        {
            _expressionReplacer.PrepareToVisit();
            leaf.AcceptExpressionVisitor(_expressionReplacer);

            if (!_expressionReplacer.ShouldReplace) {
                return leaf;
            }

            return _expressionReplacer.NewLeaf;
        }

        private IStatementLeaf ProcessStatementReplacement(IStatementLeaf leaf)
        {
            _statementReplacer.PrepareToVisit();
            leaf.AcceptStatementVisitor(_statementReplacer);

            if (!_statementReplacer.ShouldReplace) {
                return leaf;
            }

            return _statementReplacer.NewLeaf;
        }

    }
}
