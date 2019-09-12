using System.Collections.Generic;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class StackLayoutGenerator : IASTLeafVisitor
    { 
        public Dictionary<DeclarationLeaf, int> _variables = new Dictionary<DeclarationLeaf, int>();
        private int _currentOffset;

        public FunctionStackLayout GetLayout()
        {
            var variables = _variables;
            _variables = new Dictionary<DeclarationLeaf, int>();
            return new FunctionStackLayout(variables);
        }
        
        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
        }

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.GetStatement(i).Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _variables.Add(leaf, _currentOffset++);
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration.Accept(this);
            leaf.Body.Accept(this);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            foreach (DeclarationLeaf decl in leaf.Parameters) {
                VisitDeclaration(decl);
            }
            leaf.Body.Accept(this);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.IfBody.Accept(this);

            BodyLeaf elseBody;
            if (leaf.ElseBody.Get(out elseBody)) {
                elseBody.Accept(this);    
            }
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            leaf.Body.Accept(this);
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