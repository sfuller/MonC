
namespace MonC.SyntaxTree
{
    public class WhileLeaf : IASTLeaf
    {
        public readonly IASTLeaf Condition;
        public readonly IASTLeaf Body;

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