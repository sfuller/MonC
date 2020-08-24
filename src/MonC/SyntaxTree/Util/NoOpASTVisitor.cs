
namespace MonC.SyntaxTree.Util
{
    public class NoOpASTVisitor : IASTLeafVisitor
    {
        public virtual void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitBody(BodyLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitDeclaration(DeclarationLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitFor(ForLeaf leaf)
        {
            VisitDefault(leaf);   
        }

        public virtual void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitVariable(VariableLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitIfElse(IfElseLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitWhile(WhileLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitBreak(BreakLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitReturn(ReturnLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitAssignment(AssignmentLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitEnum(EnumLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitEnumValue(EnumValueLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
            VisitDefault(leaf);
        }

        public virtual void VisitDefault(IASTLeaf leaf)
        {
        }
    }
}