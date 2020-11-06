using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class StatementDelegator : IStatementVisitor
    {
        public IVisitor<BodyNode>? BodyVisitor;
        public IVisitor<DeclarationNode>? DeclarationVisitor;
        public IVisitor<BreakNode>? BreakVisitor;
        public IVisitor<ContinueNode>? ContinueVisitor;
        public IVisitor<ReturnNode>? ReturnVisitor;
        public IVisitor<IfElseNode>? IfElseVisitor;
        public IVisitor<WhileNode>? WhileVisitor;
        public IVisitor<ForNode>? ForVisitor;
        public IVisitor<ExpressionStatementNode>? ExpressionStatementVisitor;

        public void VisitBody(BodyNode node)
        {
            BodyVisitor?.Visit(node);
        }

        public void VisitDeclaration(DeclarationNode node)
        {
            DeclarationVisitor?.Visit(node);
        }

        public void VisitBreak(BreakNode node)
        {
            BreakVisitor?.Visit(node);
        }

        public void VisitContinue(ContinueNode node)
        {
            ContinueVisitor?.Visit(node);
        }

        public void VisitReturn(ReturnNode node)
        {
            ReturnVisitor?.Visit(node);
        }

        public void VisitIfElse(IfElseNode node)
        {
            IfElseVisitor?.Visit(node);
        }

        public void VisitFor(ForNode node)
        {
            ForVisitor?.Visit(node);
        }

        public void VisitWhile(WhileNode node)
        {
            WhileVisitor?.Visit(node);
        }

        public void VisitExpressionStatement(ExpressionStatementNode node)
        {
            ExpressionStatementVisitor?.Visit(node);
        }
    }
}
