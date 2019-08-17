using System;
using MonC.Parsing.Scoping;

namespace MonC.SyntaxTree.Util
{
    public class ProcessReplacementsVisitor : IASTLeafVisitor
    {
        private readonly ScopeCache _scopes;
        private IReplacementVisitor _replacer;

        public ProcessReplacementsVisitor(ScopeCache scopes = null)
        {
            _scopes = scopes;
        }

        public ProcessReplacementsVisitor SetReplacer(IReplacementVisitor replacer)
        {
            _replacer = replacer;
            return this;
        }

        public void VisitBinaryOperation(BinaryOperationExpressionLeaf leaf)
        {
            leaf.LHS = ProcessReplacement(leaf.LHS);
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.SetStatement(i, ProcessReplacement(leaf.GetStatement(i)));
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            leaf.Assignment = ProcessReplacement(leaf.Assignment);
        }

        public void VisitFor(ForLeaf leaf)
        {
            throw new NotImplementedException();
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            leaf.Body = ProcessReplacement(leaf.Body);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.SetArgument(i, ProcessReplacement(leaf.GetArgument(i)));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition = ProcessReplacement(leaf.Condition);
            leaf.IfBody = ProcessReplacement(leaf.IfBody);
            leaf.ElseBody = ProcessReplacement(leaf.ElseBody);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            leaf.Condition = ProcessReplacement(leaf.Condition);
            leaf.Body = ProcessReplacement(leaf.Body);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        private IASTLeaf ProcessReplacement(IASTLeaf leaf)
        {
            if (leaf == null) {
                return null;
            }
            
            leaf.Accept(_replacer);
            if (_replacer.ShouldReplace) {
                if (_scopes != null) {
                    Scope scope = _scopes.GetScope(leaf);
                    _scopes.SetScope(_replacer.NewLeaf, scope);
                }
                
                return _replacer.NewLeaf;
            }
            return leaf;
        }
    }
}