namespace MonC.SyntaxTree
{
    public class ReturnLeaf : IASTLeaf
    {
        public Optional<IASTLeaf> RHS;
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }
}