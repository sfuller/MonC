using System;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

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

        public void VisitBody(BodyNode node)
        {
            for (int i = 0, ilen = node.Statements.Count; i < ilen; ++i) {
                node.Statements[i] = ProcessStatementReplacement(node.Statements[i]);
            }
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            node.Assignment = ProcessExpressionReplacement(node.Assignment);
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            node.RHS = ProcessExpressionReplacement(node.RHS);
        }

        public void VisitIfElse(IfElseNode node)
        {
            node.Condition = ProcessExpressionReplacement(node.Condition);
            node.IfBody = ProcessStatementReplacement(node.IfBody);
            node.ElseBody = ProcessStatementReplacement(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration = ProcessStatementReplacement(node.Declaration);
            node.Condition = ProcessExpressionReplacement(node.Condition);
            node.Update = ProcessExpressionReplacement(node.Update);
            node.Body = ProcessStatementReplacement(node.Body);
        }

        public void VisitWhile(WhileNode node)
        {
            node.Condition = ProcessExpressionReplacement(node.Condition);
            node.Body = ProcessStatementReplacement(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression = ProcessExpressionReplacement(node.Expression);
        }

        private T ProcessExpressionReplacement<T>(T node) where T : IExpressionNode
        {
            _expressionReplacer.PrepareToVisit();
            node.AcceptExpressionVisitor(_expressionReplacer);
            return GetReplacement(node, _expressionReplacer);
        }

        private T ProcessStatementReplacement<T>(T node) where T : IStatementNode
        {
            _statementReplacer.PrepareToVisit();
            node.AcceptStatementVisitor(_statementReplacer);
            return GetReplacement(node, _statementReplacer);
        }


        private T GetReplacement<T, ReplacerT>(T node, IReplacementVisitor<ReplacerT> replacer)
            where T : ISyntaxTreeNode
            where ReplacerT : ISyntaxTreeNode
        {
            if (!replacer.ShouldReplace) {
                return node;
            }

            // TODO: Add static analysis for this.
            if (!(replacer.NewNode is T replacement)) {
                throw new InvalidOperationException("Cannot replace, type mismatch");
            }

            return replacement;
        }

    }
}
