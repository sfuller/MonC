using System;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class VariableDeclarationAnalyzer : NoOpASTVisitor
    {
        private readonly ScopeCache _scopes;
        
        public VariableDeclarationAnalyzer(ScopeCache scopes)
        {
            _scopes = scopes;
        }

        public override void VisitDeclaration(DeclarationLeaf leaf)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            Scope scope = _scopes.GetScope(leaf);
            DeclarationLeaf previousLeaf = scope.Variables.Find(existingLeaf => leaf.Name == existingLeaf.Name);

            if (previousLeaf != null) {
                throw new NotImplementedException();
            }
        }
    }
}