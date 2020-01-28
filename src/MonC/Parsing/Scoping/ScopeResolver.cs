using System;
using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;

namespace MonC.Parsing.Scoping
{
    public class ScopeResolver : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        private readonly ScopeCache _cache;
        private Scope _scope;

        public delegate void LeafHandler(IASTLeaf leaf, ScopeCache scopes, Scope scope);
         
        private readonly Dictionary<Type, LeafHandler> _extensions = new Dictionary<Type, LeafHandler>();
        
        public ScopeResolver(ScopeCache cache, Scope scope)
        {
            _cache = cache;
            _scope = scope;
        }

        public void RegisterExtension(Type type, LeafHandler handler)
        {
            _extensions.Add(type, handler);
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            VisitWithCurrentScope(leaf.LHS);
            VisitWithCurrentScope(leaf.RHS);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            VisitWithCurrentScope(leaf.RHS);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                VisitWithCurrentScope(leaf.GetStatement(i));
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            
            OptionallyVisitWithCurrentScope(leaf.Assignment);

            _scope.Variables.Add(leaf);
        }

        public void VisitFor(ForLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);

            Scope childScope = _scope.Copy();

            VisitWithScope(leaf.Declaration, childScope);
            VisitWithScope(leaf.Condition, childScope);
            VisitWithScope(leaf.Update, childScope);
            VisitWithScope(leaf.Body, childScope);
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
            OptionallyVisitChildScope(leaf.ElseBody);
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
            
            OptionallyVisitChildScope(leaf.RHS);
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
            VisitWithScope(leaf, _scope.Copy());
        }

        private void VisitWithCurrentScope(IASTLeaf leaf)
        {
            VisitWithScope(leaf, _scope);
        }

        private void OptionallyVisitWithCurrentScope(IASTLeaf? leaf)
        {
            if (leaf == null) {
                return;
            }
            VisitWithCurrentScope(leaf);
        }
        
        private void VisitWithScope(IASTLeaf leaf, Scope scope)
        {
            LeafHandler handler;
            if (_extensions.TryGetValue(leaf.GetType(), out handler)) {
                handler(leaf, _cache, scope);
                return;
            }
            Scope currentScope = _scope;
            _scope = scope;
            leaf.Accept(this);
            _scope = currentScope;
        }

        private void OptionallyVisitChildScope(IASTLeaf? leaf)
        {
            if (leaf == null) {
                return;
            }
            VisitChildScope(leaf);
        }

    }
}