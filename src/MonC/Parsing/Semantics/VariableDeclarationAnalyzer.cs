using System;
using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class VariableDeclarationAnalyzer : NoOpASTVisitor
    {
        private readonly ScopeCache _scopes;
        private readonly IList<(string message, IASTLeaf leaf)> _errors;
        
        public VariableDeclarationAnalyzer(ScopeCache scopes, IList<(string message, IASTLeaf leaf)> errors)
        {
            _scopes = scopes;
            _errors = errors;
        }

        public override void VisitDeclaration(DeclarationLeaf leaf)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            Scope scope = _scopes.GetScope(leaf);
            DeclarationLeaf previousLeaf = scope.Variables.Find(existingLeaf => leaf.Name == existingLeaf.Name);

            if (previousLeaf != null) {
                _errors.Add(($"Duplicate declaration {leaf.Name}", leaf));
            }
        }
    }
}