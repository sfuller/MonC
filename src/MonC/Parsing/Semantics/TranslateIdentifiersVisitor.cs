using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly EnumManager _enums;
        
        private readonly IList<(string name, IASTLeaf leaf)> _errors;
        private readonly IDictionary<IASTLeaf, Symbol> _symbolMap;
        
        public TranslateIdentifiersVisitor(
            ScopeCache scopes,
            Dictionary<string, FunctionDefinitionLeaf> functions,
            IList<(string name, IASTLeaf leaf)> errors,
            EnumManager enums,
            IDictionary<IASTLeaf, Symbol> symbolMap)
        {
            _scopes = scopes;
            _functions = functions;
            _enums = enums;
            _errors = errors;
            _symbolMap = symbolMap;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf? NewLeaf { get; private set; }
        
        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            ShouldReplace = true;
            
            Scope scope = _scopes.GetScope(leaf);
            DeclarationLeaf decl = scope.Variables.Find(d => d.Name == leaf.Name);
            if (decl != null) {
                NewLeaf = UpdateSymbolMap(new VariableLeaf(decl), leaf);
                return;
            }

            EnumLeaf enumLeaf = _enums.GetEnumeration(leaf.Name);
            if (enumLeaf != null) {
                NewLeaf = UpdateSymbolMap(new EnumValueLeaf(enumLeaf, leaf.Name), leaf);
                return;
            }
            
            ShouldReplace = false;
            _errors.Add(($"Undeclared identifier {leaf.Name}", leaf));
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            IdentifierParseLeaf? identifier = leaf.LHS as IdentifierParseLeaf;
            
            if (identifier == null) {
                _errors.Add(("LHS of function call operator is not an identifier.", leaf));
                return;
            }

            ShouldReplace = true;

            FunctionCallLeaf? resultLeaf = null;
            
            FunctionDefinitionLeaf function;
            if (!_functions.TryGetValue(identifier.Name, out function)) {
                _errors.Add(("Undefined function " + identifier.Name, leaf));
            } else if (function.Parameters.Length != leaf.ArgumentCount) {
                _errors.Add(($"Expected {function.Parameters.Length} argument(s), got {leaf.ArgumentCount}", leaf));
            } else {
                resultLeaf = new FunctionCallLeaf(function, leaf.GetArguments());    
            }

            if (resultLeaf == null) {
                resultLeaf = MakeFakeFunctionCall(identifier, leaf);
            }
            
            UpdateSymbolMap(resultLeaf, leaf);
            NewLeaf = resultLeaf;
        }

        private FunctionCallLeaf MakeFakeFunctionCall(IdentifierParseLeaf identifier, FunctionCallParseLeaf call)
        {
            FunctionCallLeaf fakeFunctionCall = new FunctionCallLeaf(
                lhs: new FunctionDefinitionLeaf(
                    $"(placeholder) {identifier.Name}",
                    new TypeSpecifierLeaf("int", PointerType.NotAPointer),
                    Array.Empty<DeclarationLeaf>(),
                    new BodyLeaf(Array.Empty<IASTLeaf>()),
                    isExported: false
                ),
                arguments: Enumerable.Range(0, call.ArgumentCount).Select(call.GetArgument));
            
            return fakeFunctionCall;
        }

        public override void VisitDefault(IASTLeaf leaf)
        {
            ShouldReplace = false;
        }

        private IASTLeaf UpdateSymbolMap(IASTLeaf leaf, IASTLeaf original)
        {
            Symbol originalSymbol;
            _symbolMap.TryGetValue(original, out originalSymbol);
            _symbolMap[leaf] = originalSymbol;
            return leaf;
        }
    }
}