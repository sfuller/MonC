using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;

namespace MonC.Parsing.Scoping
{
    public class ScopeResolver : IASTLeafVisitor, IParseTreeLeafVisitor
    {
        //private readonly IASTLeaf _context;
        private readonly ScopeCache _cache;
        private readonly Scope _scope;
        //private bool _foundContext;

        public ScopeResolver(ScopeCache cache, Scope parentScope)
        {
            _cache = cache;
            _scope = parentScope.Copy();
        }
        
//        public Scope Scope {
//            get { return _scope; }
//        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            leaf.LHS.Accept(this);
            leaf.RHS.Accept(this);
            
//            CheckChild(leaf.LHS);
//            CheckChild(leaf.RHS);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            leaf.RHS.Accept(this);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            //var visitor = new ScopeResolver(_context);
            
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.GetStatement(i).Accept(this);
//                if (_foundContext) {
//                    break;
//                }
            }
            
            
            //_scope.Variables.AddRange(visitor._scope.Variables);
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }

            _cache.SetScope(leaf, _scope);

            IASTLeaf assignment;
            if (leaf.Assignment.Get(out assignment)) {
                assignment.Accept(this);
            }

            _scope.Variables.Add(leaf);
        }

        public void VisitFor(ForLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            leaf.Declaration.Accept(this);
            leaf.Condition.Accept(this);
            leaf.Update.Accept(this);
            
            CheckChild(leaf.Body);
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }

            _cache.SetScope(leaf, _scope);
            
            foreach (DeclarationLeaf decl in leaf.Parameters) {
                _scope.Variables.Add(decl);
            }
            
            CheckChild(leaf.Body);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                CheckChild(leaf.GetArgument(i));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            _cache.SetScope(leaf, _scope);
            CheckContext(leaf);
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            CheckChild(leaf.Condition);
            CheckChild(leaf.IfBody);
            CheckChild(leaf.ElseBody);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            CheckContext(leaf);
            _cache.SetScope(leaf, _scope);
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            CheckContext(leaf);
            _cache.SetScope(leaf, _scope);
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            _cache.SetScope(leaf, _scope);

            CheckChild(leaf.Condition);
            CheckChild(leaf.Body);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            CheckContext(leaf);
            _cache.SetScope(leaf, _scope);
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            CheckChild(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            if (CheckContext(leaf)) {
                return;
            }
            
            _cache.SetScope(leaf, _scope);
            
            CheckChild(leaf.RHS);
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
        
        private bool CheckContext(IASTLeaf leaf)
        {
//            if (_context == leaf) {
//                _foundContext = true;
//                return true;
//            }
//            return false;
            return false;
        }

        private void CheckChild(IASTLeaf leaf)
        {
            var visitor = new ScopeResolver(_cache, _scope);
            leaf.Accept(visitor);
            
//            if (visitor._foundContext || leaf == _context) {
//                _foundContext = true;
//                _scope.Variables.AddRange(visitor._scope.Variables);
//            }
        }

//        private void CheckChild(Optional<IASTLeaf> leaf)
//        {
//            IASTLeaf nonNullLeaf;
//            if (leaf.Get(out nonNullLeaf)) {
//                CheckChild(nonNullLeaf);
//            }
//        }

        private void CheckChild<T>(Optional<T> leaf) where T : class, IASTLeaf
        {
            T nonNullLeaf;
            if (leaf.Get(out nonNullLeaf)) {
                CheckChild(nonNullLeaf);
            }
        }

    }
}