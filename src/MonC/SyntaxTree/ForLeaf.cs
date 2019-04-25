
namespace MonC.SyntaxTree
{
    public class ForLeaf : IASTLeaf
    {
        public IASTLeaf Declaration;
        public IASTLeaf Condition;
        public IASTLeaf Update;
        public IASTLeaf Body;

        public ForLeaf(IASTLeaf declaration, IASTLeaf condition, IASTLeaf update, IASTLeaf body)
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