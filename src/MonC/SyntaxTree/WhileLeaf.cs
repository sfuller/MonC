
namespace MonC.SyntaxTree
{
    public class WhileLeaf : IASTLeaf
    {
        public IASTLeaf Condition;
        public BodyLeaf Body;

        public WhileLeaf(IASTLeaf condition, BodyLeaf body)
        {
            Condition = condition;
            Body = body;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitWhile(this);
        }
    }
}