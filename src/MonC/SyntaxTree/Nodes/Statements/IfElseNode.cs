namespace MonC.SyntaxTree.Nodes.Statements
{
    public class IfElseNode : StatementNode
    {
        public IExpressionNode Condition;
        public BodyNode IfBody;
        public BodyNode ElseBody;

        public IfElseNode(IExpressionNode condition, BodyNode ifBody, BodyNode elseBody)
        {
            Condition = condition;
            IfBody = ifBody;
            ElseBody = elseBody;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitIfElse(this);
        }

    }
}
