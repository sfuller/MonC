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
        private readonly IList<ParseError> _errors;

        public ProcessAssignmentsVisitor(ScopeCache scopes, IList<ParseError> errors)
        {
            _scopes = scopes;
            _errors = errors;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf NewLeaf { get; private set; }

        public override void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            base.VisitBinaryOperation(leaf);
            
            if (leaf.Op.Value == "=") {
                IdentifierParseLeaf identifier = leaf.LHS as IdentifierParseLeaf;
                if (identifier == null) {
                    // TODO: Make this shared functionality
                    GetTokenVisitor tokenVisitor = new GetTokenVisitor();
                    leaf.LHS.Accept(tokenVisitor);
                    _errors.Add(new ParseError {Message = "Expecting identifier", Token = tokenVisitor.Token} );
                    return;
                }

                Scope scope = _scopes.GetScope(leaf);
                
                DeclarationLeaf declaration = scope.Variables.Find(d => d.Name == identifier.Name);
                if (declaration == null) {
                    _errors.Add(new ParseError {Message = $"Undeclared identifier {identifier.Name}", Token = identifier.Token} );
                    return;
                }

                ShouldReplace = true;
                NewLeaf = new AssignmentLeaf {Declaration = declaration, RHS = leaf.RHS};
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