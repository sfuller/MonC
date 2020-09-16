namespace MonC.SyntaxTree.Leaves.Statements
{
    public class ForLeaf : IStatementLeaf
    {
        public DeclarationLeaf Declaration;
        public IExpressionLeaf Condition;
        public IExpressionLeaf Update;
        public BodyLeaf Body;

        public ForLeaf(DeclarationLeaf declaration, IExpressionLeaf condition, IExpressionLeaf update, BodyLeaf body)
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
