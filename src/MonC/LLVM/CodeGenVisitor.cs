using System;
using MonC.SyntaxTree;

namespace MonC.LLVM
{
    public class CodeGenVisitor : IASTLeafVisitor
    {
        private CodeGeneratorContext _genContext;
        private CodeGeneratorContext.Function _function;
        private Builder? _builder;
        private BasicBlock _basicBlock;

        internal CodeGenVisitor(CodeGeneratorContext genContext, CodeGeneratorContext.Function function)
        {
            _genContext = genContext;
            _function = function;
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
                IASTLeaf statement = leaf.GetStatement(i);
                statement.Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
        }

        public void VisitFor(ForLeaf leaf)
        {
        }

        private bool _hasVisitedFunctionDefinition;

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            if (_hasVisitedFunctionDefinition) {
                // This should only be called once, for the function definition being generated.
                throw new InvalidOperationException("Attempting to visit another function definition.");
            }

            _hasVisitedFunctionDefinition = true;

            using (Builder builder = _genContext.Context.CreateBuilder()) {
                _builder = builder;
                _basicBlock = _function.StartDefinition(_genContext);
                _builder.PositionAtEnd(_basicBlock);
                leaf.Body.Accept(this);
                _builder = null;
            }
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

        public void VisitContinue(ContinueLeaf leaf)
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

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
        }
    }
}