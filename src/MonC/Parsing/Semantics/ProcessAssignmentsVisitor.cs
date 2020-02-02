using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class ProcessAssignmentsVisitor : NoOpASTVisitor, IReplacementVisitor, IParseTreeLeafVisitor
    {
        private readonly ScopeCache _scopes;
        private readonly IList<(string message, IASTLeaf leaf)> _errors;
        private readonly IDictionary<IASTLeaf, Symbol> _symbolMap;

        public ProcessAssignmentsVisitor(ScopeCache scopes, IList<(string message, IASTLeaf leaf)> errors, IDictionary<IASTLeaf, Symbol> symbolMap)
        {
            _scopes = scopes;
            _errors = errors;
            _symbolMap = symbolMap;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf? NewLeaf { get; private set; }

        public override void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            base.VisitBinaryOperation(leaf);
            
            if (leaf.Op.Value == "=") {
                IdentifierParseLeaf? identifier = leaf.LHS as IdentifierParseLeaf;
                if (identifier == null) {
                    _errors.Add(("Expecting identifier", leaf.LHS));
                    return;
                }

                Scope scope = _scopes.GetScope(leaf);

                ShouldReplace = true;
                AssignmentLeaf? resultLeaf = null;
                
                DeclarationLeaf declaration = scope.Variables.Find(d => d.Name == identifier.Name);
                if (declaration == null) {
                    _errors.Add(($"Undeclared identifier {identifier.Name}", identifier));
                } else {
                    resultLeaf = new AssignmentLeaf(declaration, leaf.RHS);
                }

                if (resultLeaf == null) {
                    DeclarationLeaf fakeDeclaration = new DeclarationLeaf(
                        type: "int",
                        name: $"(undefined){identifier.Name}",
                        assignment: null
                    );
                    resultLeaf = new AssignmentLeaf(fakeDeclaration, leaf.RHS);
                }
                
                NewLeaf = resultLeaf;
                
                // TODO: Need more automated symbol association for new leaves.
                Symbol originalSymbol;
                _symbolMap.TryGetValue(leaf, out originalSymbol);
                _symbolMap[NewLeaf] = originalSymbol;
            }
        }

        public override void VisitDefault(IASTLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitIdentifier(IdentifierParseLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitFunctionCall(FunctionCallParseLeaf leaf)
        {
            ShouldReplace = false;
        }
    }
}