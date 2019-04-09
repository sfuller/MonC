using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class ScopeVisitor : IASTLeafVisitor
    {
       
        
        
        
        
        
        
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitFor(ForLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitFunction(FunctionLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitIdentifier(IdentifierLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitPlaceholder(PlaceholderLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitStringLiteral(StringLIteralLeaf leaf)
        {
            throw new System.NotImplementedException();
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            throw new System.NotImplementedException();
        }
    }
}