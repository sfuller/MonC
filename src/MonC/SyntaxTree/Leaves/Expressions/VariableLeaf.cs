using MonC.SyntaxTree.Leaves.Statements;

namespace MonC.SyntaxTree.Leaves.Expressions
{
    public class VariableLeaf : IExpressionLeaf
    {
        public DeclarationLeaf Declaration;

        public VariableLeaf(DeclarationLeaf declaration)
        {
            Declaration = declaration;
        }

        public void AcceptExpressionVisitor(IExpressionVisitor visitor)
        {
            visitor.VisitVariable(this);
        }
    }
}
