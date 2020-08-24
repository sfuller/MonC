

namespace MonC.SyntaxTree
{
    public class VariableLeaf : IASTLeaf
    {
        public DeclarationLeaf Declaration;

        public VariableLeaf(DeclarationLeaf declaration)
        {
            Declaration = declaration;
        }
        
        public void Accept(IASTLeafVisitor visitor)
        {
            visitor.VisitVariable(this);
        }
    }
}