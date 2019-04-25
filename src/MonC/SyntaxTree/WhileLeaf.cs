
namespace MonC.SyntaxTree
{
    public class WhileLeaf : IASTLeaf
    {
        public IASTLeaf Condition;
        public IASTLeaf Body;

        public WhileLeaf(IASTLeaf condition, IASTLeaf body)
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