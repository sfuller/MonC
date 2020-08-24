namespace MonC.SyntaxTree
{
    public class BreakLeaf : IASTLeaf
    {
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitBreak(this);
        }
    }
}