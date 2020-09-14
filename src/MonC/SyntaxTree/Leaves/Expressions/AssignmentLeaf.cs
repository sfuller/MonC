using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class AssignmentLeaf : IExpressionLeaf
    {
        public DeclarationLeaf Declaration;
        public IExpressionLeaf RHS;

        public AssignmentLeaf(DeclarationLeaf declaration, IExpressionLeaf rhs)
        {
            Declaration = declaration;
            RHS = rhs;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }

    }
}
