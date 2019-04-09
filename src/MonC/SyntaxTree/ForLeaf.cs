
namespace MonC.SyntaxTree
{
    public class ForLeaf : IASTLeaf
    {
        public readonly IASTLeaf Declaration;
        public readonly IASTLeaf Condition;
        public readonly IASTLeaf Update;
        public readonly IASTLeaf Body;

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