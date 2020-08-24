namespace MonC.SyntaxTree
{
    public class ContinueLeaf : IASTLeaf
    {
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitContinue(this);
        }
    }
}