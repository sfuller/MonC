namespace MonC.SyntaxTree
{
    public class PlaceholderLeaf : IASTLeaf
    {
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitPlaceholder(this);
        }
    }
}