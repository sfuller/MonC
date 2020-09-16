using System;
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

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Statements.Count; i < ilen; ++i) {
                leaf.Statements[i] = ProcessStatementReplacement(leaf.Statements[i]);
            }
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
            leaf.IfBody = ProcessStatementReplacement(leaf.IfBody);
            leaf.ElseBody = ProcessStatementReplacement(leaf.ElseBody);
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration = ProcessStatementReplacement(leaf.Declaration);
            leaf.Condition = ProcessExpressionReplacement(leaf.Condition);
            leaf.Update = ProcessExpressionReplacement(leaf.Update);
            leaf.Body = ProcessStatementReplacement(leaf.Body);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            leaf.Condition = ProcessExpressionReplacement(leaf.Condition);
            leaf.Body = ProcessStatementReplacement(leaf.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementLeaf leaf)
        {
            leaf.Expression = ProcessExpressionReplacement(leaf.Expression);
        }

        private T ProcessExpressionReplacement<T>(T leaf) where T : IExpressionLeaf
        {
            _expressionReplacer.PrepareToVisit();
            leaf.AcceptExpressionVisitor(_expressionReplacer);
            return GetReplacement(leaf, _expressionReplacer);
        }

        private T ProcessStatementReplacement<T>(T leaf) where T : IStatementLeaf
        {
            _statementReplacer.PrepareToVisit();
            leaf.AcceptStatementVisitor(_statementReplacer);
            return GetReplacement(leaf, _statementReplacer);
        }


        private T GetReplacement<T, ReplacerT>(T leaf, IReplacementVisitor<ReplacerT> replacer)
            where T : ISyntaxTreeLeaf
            where ReplacerT : ISyntaxTreeLeaf
        {
            if (!replacer.ShouldReplace) {
                return leaf;
            }

            // TODO: Add static analysis for this.
            if (!(replacer.NewLeaf is T replacement)) {
                throw new InvalidOperationException("Cannot replace, type mismatch");
            }

            return replacement;
        }

    }
}
