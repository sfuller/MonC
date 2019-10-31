using System;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class VariableAnalyzer : NoOpASTVisitor, IParseTreeLeafVisitor, IReplacementVisitor
    {
        private readonly ScopeCache _scopes;

        public VariableAnalyzer(ScopeCache scopes)
        {
            _scopes = scopes;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf? NewLeaf { get; private set; }

        public override void VisitDefault(IASTLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            Scope scope = _scopes.GetScope(leaf);
            DeclarationLeaf declaration = scope.Variables.Find(d => leaf.Name == d.Name);
            
            if (declaration == null) {
                throw new NotImplementedException();
            }
            
            ShouldReplace = true;
            NewLeaf = new VariableLeaf(declaration);
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            base.VisitDefault(leaf);
        }
    }
}