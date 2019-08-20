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

        private IList<ParseError> _errors;

        public TranslateIdentifiersVisitor(ScopeCache scopes, Dictionary<string, FunctionDefinitionLeaf> functions, IList<ParseError> errors)
        {
            _scopes = scopes;
            _functions = functions;
            _errors = errors;
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
                _errors.Add(new ParseError {
                    Message = "LHS of function call operator is not an identifier.",
                    Token = new Token()  // TODO
                });
                return;
            }

            FunctionDefinitionLeaf function;
            if (!_functions.TryGetValue(identifier.Name, out function)) {
                _errors.Add(new ParseError {
                    Message = "Undefined function " + identifier.Name,
                    Token = new Token() // TODO
                });
                return;
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