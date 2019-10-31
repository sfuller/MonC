namespace MonC.SyntaxTree
{
    public class AssignmentLeaf : IASTLeaf
    {
        public DeclarationLeaf Declaration;
        public IASTLeaf RHS;

        public AssignmentLeaf(DeclarationLeaf declaration, IASTLeaf rhs)
        {
            Declaration = declaration;
            RHS = rhs;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }
}