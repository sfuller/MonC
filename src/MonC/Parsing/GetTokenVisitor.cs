using MonC.SyntaxTree;

namespace MonC.Parsing
{
    public class GetTokenVisitor : IASTLeafVisitor
    {
        public Token Token { get; private set; }
        
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            leaf.LHS.Accept(this);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            Token = leaf.Operator;
        }

        public void VisitBody(BodyLeaf leaf)
        {
            //Token = leaf.Token;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            //Token = leaf.Token;
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.Accept(this);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
        }

        public void VisitEnum(EnumLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }
    }
}