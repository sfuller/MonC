namespace MonC.SyntaxTree
{
    public class IfElseLeaf : IASTLeaf
    {
        public IASTLeaf Condition;
        public BodyLeaf IfBody;
        public BodyLeaf? ElseBody;

        public IfElseLeaf(IASTLeaf condition, BodyLeaf ifBody, BodyLeaf? elseBody)
        {
            Condition = condition;
            IfBody = ifBody;
            ElseBody = elseBody;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitIfElse(this);
        }
    }
}