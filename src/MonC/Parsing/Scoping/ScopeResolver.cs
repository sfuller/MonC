using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;

namespace MonC.Parsing.Scoping
{
    public class ScopeResolver : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        private readonly ScopeCache _cache;
        private readonly Scope _scope;

        public ScopeResolver(ScopeCache cache, Scope scope)
        {
            _cache = cache;
            _scope = scope;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            leaf.LHS.Accept(this);
            leaf.RHS.Accept(this);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            leaf.RHS.Accept(this);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.GetStatement(i).Accept(this);
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            IASTLeaf assignment;
            if (leaf.Assignment.Get(out assignment)) {
                assignment.Accept(this);
            }

            _scope.Variables.Add(leaf);
        }

        public void VisitFor(ForLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            Scope childScope = _scope.Copy();
            ScopeResolver childVisitor = new ScopeResolver(_cache, childScope);
            
            leaf.Declaration.Accept(childVisitor);
            leaf.Condition.Accept(childVisitor);
            leaf.Update.Accept(childVisitor);
            leaf.Body.Accept(childVisitor);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            foreach (DeclarationLeaf decl in leaf.Parameters) {
                _scope.Variables.Add(decl);
            }
            
            VisitChildScope(leaf.Body);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                VisitChildScope(leaf.GetArgument(i));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            VisitChildScope(leaf.Condition);
            VisitChildScope(leaf.IfBody);
            VisitChildScope(leaf.ElseBody);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            VisitChildScope(leaf.Condition);
            VisitChildScope(leaf.Body);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            VisitChildScope(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            VisitChildScope(leaf.RHS);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
        }

        private void VisitChildScope(IASTLeaf leaf)
        {
            var visitor = new ScopeResolver(_cache, _scope.Copy());
            leaf.Accept(visitor);
        }

        private void VisitChildScope<T>(Optional<T> leaf) where T : class, IASTLeaf
        {
            T nonNullLeaf;
            if (leaf.Get(out nonNullLeaf)) {
                VisitChildScope(nonNullLeaf);
            }
        }

    }
}