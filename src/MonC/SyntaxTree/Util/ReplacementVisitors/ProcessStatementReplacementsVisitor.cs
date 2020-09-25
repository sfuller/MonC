using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessStatementReplacementsVisitor : IStatementVisitor
    {
        public readonly ReplacementProcessor _processor;

        public ProcessStatementReplacementsVisitor(IReplacementSource replacementSource)
        {
            _processor = new ReplacementProcessor(replacementSource);
        }

        public void VisitBody(BodyNode node)
        {
            for (int i = 0, ilen = node.Statements.Count; i < ilen; ++i) {
                node.Statements[i] = _processor.ProcessReplacement(node.Statements[i]);
            }
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            node.Type = _processor.ProcessReplacement(node.Type);
            node.Assignment = _processor.ProcessReplacement(node.Assignment);
        }

        public void VisitBreak(BreakNode node)
        {
        }

        public void VisitContinue(ContinueNode node)
        {
        }

        public void VisitReturn(ReturnNode node)
        {
            node.RHS = _processor.ProcessReplacement(node.RHS);
        }

        public void VisitIfElse(IfElseNode node)
        {
            node.Condition = _processor.ProcessReplacement(node.Condition);
            node.IfBody = _processor.ProcessReplacement(node.IfBody);
            node.ElseBody = _processor.ProcessReplacement(node.ElseBody);
        }

        public void VisitFor(ForNode node)
        {
            node.Declaration = _processor.ProcessReplacement(node.Declaration);
            node.Condition = _processor.ProcessReplacement(node.Condition);
            node.Update = _processor.ProcessReplacement(node.Update);
            node.Body = _processor.ProcessReplacement(node.Body);
        }

        public void VisitWhile(WhileNode node)
        {
            node.Condition = _processor.ProcessReplacement(node.Condition);
            node.Body = _processor.ProcessReplacement(node.Body);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            node.Expression = _processor.ProcessReplacement(node.Expression);
        }
    }
}
