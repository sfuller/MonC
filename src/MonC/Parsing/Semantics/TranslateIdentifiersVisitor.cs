using System;
using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class TranslateIdentifiersVisitor : NoOpASTVisitor, IParseTreeLeafVisitor, IReplacementVisitor
    {
        private readonly ScopeCache _scopes;
        private readonly Dictionary<string, FunctionDefinitionLeaf> _functions;

        public TranslateIdentifiersVisitor(ScopeCache scopes, Dictionary<string, FunctionDefinitionLeaf> functions)
        {
            _scopes = scopes;
            _functions = functions;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf NewLeaf { get; private set; }
        
        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            ShouldReplace = false;
            
            Scope scope = _scopes.GetScope(leaf);
            DeclarationLeaf decl = scope.Variables.Find(d => d.Name == leaf.Name);
            if (decl == null) {
                return;
            }

            ShouldReplace = true;
            NewLeaf = new VariableLeaf(decl);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            IdentifierParseLeaf identifier = leaf.LHS as IdentifierParseLeaf;

            if (identifier == null) {
                throw new NotImplementedException();
            }

            FunctionDefinitionLeaf function;
            if (!_functions.TryGetValue(identifier.Name, out function)) {
                throw new NotImplementedException();
            }

            ShouldReplace = true;
            NewLeaf = new FunctionCallLeaf(function, leaf.GetArguments());
        }

        public override void VisitDefault(IASTLeaf leaf)
        {
            ShouldReplace = false;
        }
    }
}