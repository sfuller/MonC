
namespace MonC.SyntaxTree
{
    public class ForLeaf : IASTLeaf
    {
        public DeclarationLeaf Declaration;
        public IASTLeaf Condition;
        public IASTLeaf Update;
        public BodyLeaf Body;

        public ForLeaf(DeclarationLeaf declaration, IASTLeaf condition, IASTLeaf update, BodyLeaf body)
        {
            Declaration = declaration;
            Condition = condition;
            Update = update;
            Body = body;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitFor(this);
        }
    }
}