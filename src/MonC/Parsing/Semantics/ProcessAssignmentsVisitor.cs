using System;
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

        public ProcessAssignmentsVisitor(ScopeCache scopes, IList<(string message, IASTLeaf leaf)> errors)
        {
            _scopes = scopes;
            _errors = errors;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf? NewLeaf { get; private set; }

        public override void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            base.VisitBinaryOperation(leaf);
            
            if (leaf.Op.Value == "=") {
                IdentifierParseLeaf? identifier = leaf.LHS as IdentifierParseLeaf;
                if (identifier == null) {
                    // TODO: Make this shared functionality
                    GetTokenVisitor tokenVisitor = new GetTokenVisitor();
                    leaf.LHS.Accept(tokenVisitor);
                    _errors.Add(("Expecting identifier", leaf.LHS));
                    return;
                }

                Scope scope = _scopes.GetScope(leaf);
                
                DeclarationLeaf declaration = scope.Variables.Find(d => d.Name == identifier.Name);
                if (declaration == null) {
                    _errors.Add(($"Undeclared identifier {identifier.Name}", identifier));
                    return;
                }

                ShouldReplace = true;
                NewLeaf = new AssignmentLeaf(declaration, leaf.RHS);
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