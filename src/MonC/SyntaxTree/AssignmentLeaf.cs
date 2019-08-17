namespace MonC.SyntaxTree
{
    public class AssignmentLeaf : IASTLeaf
    {
        public DeclarationLeaf Declaration;
        public IASTLeaf RHS;
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }
}