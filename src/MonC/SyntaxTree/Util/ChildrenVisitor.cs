using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;

namespace MonC.SyntaxTree.Util
{
    public class ChildrenVisitor : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        private IASTLeafVisitor _visitor;

        /// <summary>
        /// This outer visitor may be set to allow VisitChildrenVisitor to work with a Similar Children Visitor for
        /// extensions to the standard leaves.
        /// </summary>
        private readonly IASTLeafVisitor _outerVisitor;
        
        public ChildrenVisitor(IASTLeafVisitor? visitor = null, IASTLeafVisitor? outerVisitor = null)
        {
            _visitor = visitor ?? new NoOpASTVisitor();
            _outerVisitor = outerVisitor ?? this;
        }

        public ChildrenVisitor SetVisitor(IASTLeafVisitor? visitor)
        {
            _visitor = visitor ?? new NoOpASTVisitor();
            return this;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            _visitor.VisitBinaryOperation(leaf);
            
            leaf.LHS.Accept(_outerVisitor);
            leaf.RHS.Accept(_outerVisitor);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            _visitor.VisitUnaryOperation(leaf);
            
            leaf.RHS.Accept(_outerVisitor);
        }
        
        public void VisitAssignment(AssignmentLeaf leaf)
        {
            _visitor.VisitAssignment(leaf);
            leaf.RHS.Accept(_outerVisitor);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            _visitor.VisitEnum(leaf);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            _visitor.VisitEnumValue(leaf);
        }

        public void VisitTypeSpecifier(TypeSpecifierLeaf leaf)
        {
            _visitor.VisitTypeSpecifier(leaf);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            _visitor.VisitBody(leaf);

            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.GetStatement(i).Accept(_outerVisitor);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _visitor.VisitDeclaration(leaf);

            leaf.Assignment?.Accept(_outerVisitor);
        }

        public void VisitFor(ForLeaf leaf)
        {
            _visitor.VisitFor(leaf);
            
            leaf.Declaration.Accept(_outerVisitor);
            leaf.Condition.Accept(_outerVisitor);
            leaf.Update.Accept(_outerVisitor);
            leaf.Body.Accept(_outerVisitor);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            _visitor.VisitFunctionDefinition(leaf);

            for (int i = 0, ilen = leaf.Parameters.Length; i < ilen; ++i) {
                leaf.Parameters[i].Accept(_outerVisitor);
            }
        
            leaf.Body?.Accept(_outerVisitor);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            _visitor.VisitFunctionCall(leaf);

            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(_outerVisitor);
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            _visitor.VisitVariable(leaf);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            _visitor.VisitIfElse(leaf);
            
            leaf.Condition.Accept(_outerVisitor);
            leaf.IfBody.Accept(_outerVisitor);
            leaf.ElseBody?.Accept(_outerVisitor);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            _visitor.VisitNumericLiteral(leaf);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            _visitor.VisitStringLiteral(leaf);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            _visitor.VisitWhile(leaf);
            
            leaf.Condition.Accept(_outerVisitor);
            leaf.Body.Accept(_outerVisitor);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            _visitor.VisitBreak(leaf);
        }

        public void VisitContinue(ContinueLeaf leaf)
        {
            _visitor.VisitContinue(leaf);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            _visitor.VisitReturn(leaf);

            leaf.RHS?.Accept(_outerVisitor);
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            leaf.Accept(_visitor);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            leaf.Accept(_visitor);

            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(_outerVisitor);
            }
        }

    }
}