namespace MonC.SyntaxTree.Nodes.Statements
{
    public class ForNode : StatementNode
    {
        public DeclarationNode Declaration;
        public IExpressionNode Condition;
        public IExpressionNode Update;
        public BodyNode Body;

        public ForNode(DeclarationNode declaration, IExpressionNode condition, IExpressionNode update, BodyNode body)
        {
            Declaration = declaration;
            Condition = condition;
            Update = update;
            Body = body;
        }

        public override void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitFor(this);
        }

    }
}
