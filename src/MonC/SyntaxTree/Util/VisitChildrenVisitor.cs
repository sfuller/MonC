using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;

namespace MonC.SyntaxTree.Util
{
public class VisitChildrenVisitor : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        private IASTLeafVisitor _visitor;
        private IParseTreeLeafVisitor _parseTreeVisitor;

        public VisitChildrenVisitor SetASTVisitor(IASTLeafVisitor visitor)
        {
            if (visitor == null) {
                _visitor = new NoOpASTVisitor();
            } else {
                _visitor = visitor;                
            }
            return this;
        }

        public VisitChildrenVisitor SetParseTreeVisitor(IParseTreeLeafVisitor visitor)
        {
            if (visitor == null) {
                _parseTreeVisitor = new NoOpParseTreeVisitor();
            } else {
                _parseTreeVisitor = visitor;
            }
            
            return this;
        }

        public VisitChildrenVisitor SetVisitors(object visitor)
        {
            SetASTVisitor(visitor as IASTLeafVisitor);
            SetParseTreeVisitor(visitor as IParseTreeLeafVisitor);
            return this;
        }

        public VisitChildrenVisitor(IASTLeafVisitor visitor = null, IParseTreeLeafVisitor parseTreeVisitor = null)
        {
            SetASTVisitor(visitor);
            SetParseTreeVisitor(parseTreeVisitor);
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            _visitor.VisitBinaryOperation(leaf);
            
            leaf.LHS.Accept(this);
            leaf.RHS.Accept(this);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            _visitor.VisitUnaryOperation(leaf);
            
            leaf.RHS.Accept(this);
        }

        void IASTLeafVisitor.VisitBody(BodyLeaf leaf)
        {
            VisitBody(leaf);
        }

        void IASTLeafVisitor.VisitDeclaration(DeclarationLeaf leaf)
        {
            VisitDeclaration(leaf);
        }

        void IASTLeafVisitor.VisitFor(ForLeaf leaf)
        {
            VisitFor(leaf);
        }

        void IASTLeafVisitor.VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            VisitFunctionDefinition(leaf);
        }

        void IASTLeafVisitor.VisitFunctionCall(FunctionCallLeaf leaf)
        {
            VisitFunctionCall(leaf);
        }

        void IASTLeafVisitor.VisitVariable(VariableLeaf leaf)
        {
            VisitVariable(leaf);
        }

        void IASTLeafVisitor.VisitIfElse(IfElseLeaf leaf)
        {
            VisitIfElse(leaf);
        }

        void IASTLeafVisitor.VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            VisitNumericLiteral(leaf);
        }

        void IASTLeafVisitor.VisitStringLiteral(StringLiteralLeaf leaf)
        {
            VisitStringLiteral(leaf);
        }

        void IASTLeafVisitor.VisitWhile(WhileLeaf leaf)
        {
            VisitWhile(leaf);
        }

        void IASTLeafVisitor.VisitBreak(BreakLeaf leaf)
        {
            VisitBreak(leaf);
        }

        void IASTLeafVisitor.VisitReturn(ReturnLeaf leaf)
        {
            VisitReturn(leaf);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            _visitor.VisitAssignment(leaf);
            leaf.RHS.Accept(this);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
            _visitor.VisitEnum(leaf);
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
            _visitor.VisitEnumValue(leaf);
        }
        
        public void VisitBody(BodyLeaf leaf)
        {
            _visitor.VisitBody(leaf);

            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.GetStatement(i).Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _visitor.VisitDeclaration(leaf);

            IASTLeaf assignment;
            if (leaf.Assignment.Get(out assignment)) {
                assignment.Accept(this);    
            }
        }

        public void VisitFor(ForLeaf leaf)
        {
            _visitor.VisitFor(leaf);
            
            leaf.Declaration.Accept(this);
            leaf.Condition.Accept(this);
            leaf.Update.Accept(this);
            leaf.Body.Accept(this);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            _visitor.VisitFunctionDefinition(leaf);

            for (int i = 0, ilen = leaf.Parameters.Length; i < ilen; ++i) {
                leaf.Parameters[i].Accept(this);
            }

            if (leaf.Body != null) {
                leaf.Body.Accept(this);    
            }
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            _visitor.VisitFunctionCall(leaf);

            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            _visitor.VisitVariable(leaf);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            _visitor.VisitIfElse(leaf);
            
            leaf.Condition.Accept(this);
            leaf.IfBody.Accept(this);

            BodyLeaf elseBody;
            if (leaf.ElseBody.Get(out elseBody)) {
                elseBody.Accept(this);    
            }
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
            
            leaf.Condition.Accept(this);
            leaf.Body.Accept(this);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            _visitor.VisitBreak(leaf);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            _visitor.VisitReturn(leaf);

            IASTLeaf rhs;
            if (leaf.RHS.Get(out rhs)) {
                rhs.Accept(this);
            }
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            _parseTreeVisitor.VisitIdentifier(leaf);
        }

        void IParseTreeLeafVisitor.VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            VisitFunctionCall(leaf);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            _parseTreeVisitor.VisitFunctionCall(leaf);

            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.GetArgument(i).Accept(this);
            }
        }

    }
}