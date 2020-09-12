namespace MonC.SyntaxTree.Leaves.Statements
{
    public class ForLeaf : IStatementLeaf
    {
        public DeclarationLeaf Declaration;
        public IExpressionLeaf Condition;
        public IExpressionLeaf Update;
        public Body Body;

        public ForLeaf(DeclarationLeaf declaration, IExpressionLeaf condition, IExpressionLeaf update, Body body)
        {
            Declaration = declaration;
            Condition = condition;
            Update = update;
            Body = body;
        }

        public void AcceptStatementVisitor(IStatementVisitor visitor)
        {
            visitor.VisitFor(this);
        }

    }
}
