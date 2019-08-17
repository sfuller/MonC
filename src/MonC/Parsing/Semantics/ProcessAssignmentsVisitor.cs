using System;
using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.Semantics
{
    public class ProcessAssignmentsVisitor : IReplacementVisitor, IParseTreeLeafVisitor
    {
        private readonly ScopeCache _scopes;

        public ProcessAssignmentsVisitor(ScopeCache scopes)
        {
            _scopes = scopes;
        }

        public bool ShouldReplace { get; private set; }
        public IASTLeaf NewLeaf { get; private set; }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            if (leaf.Op.Value == "=") {
                IdentifierParseLeaf identifier = leaf.LHS as IdentifierParseLeaf;
                if (identifier == null) {
                    throw new NotImplementedException("TODO");    
                }

                Scope scope = _scopes.GetScope(leaf);
                
                DeclarationLeaf declaration = scope.Variables.Find(d => d.Name == identifier.Name);
                if (declaration == null) {
                    throw new NotImplementedException("TODO");
                }

                ShouldReplace = true;
                NewLeaf = new AssignmentLeaf {Declaration = declaration, RHS = leaf.RHS};
            }
        }

        public void VisitBody(BodyLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitFor(ForLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitVariable(VariableLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitBreak(BreakLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            ShouldReplace = false;
        }

        public void VisitAssignment(AssignmentLeaf leaf)
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