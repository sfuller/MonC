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
            leaf.LHS = ProcessReplacement(leaf.LHS, leaf);
            leaf.RHS = ProcessReplacement(leaf.RHS, leaf);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                leaf.SetStatement(i, ProcessReplacement(leaf.GetStatement(i), leaf));
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            leaf.Assignment = ProcessReplacement(leaf.Assignment, leaf);
        }

        public void VisitFor(ForLeaf leaf)
        {
            throw new NotImplementedException();
        }

        public void VisitFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            leaf.Body = ProcessReplacement(leaf.Body, leaf);
        }

        public void VisitFunctionCall(FunctionCallLeaf leaf)
        {
            for (int i = 0, ilen = leaf.ArgumentCount; i < ilen; ++i) {
                leaf.SetArgument(i, ProcessReplacement(leaf.GetArgument(i), leaf));
            }
        }

        public void VisitVariable(VariableLeaf leaf)
        {
        }

        public void VisitIfElse(IfElseLeaf leaf)
        {
            leaf.Condition = ProcessReplacement(leaf.Condition, leaf);
            leaf.IfBody = ProcessReplacement(leaf.IfBody, leaf);
            leaf.ElseBody = ProcessReplacement(leaf.ElseBody, leaf);
        }

        public void VisitNumericLiteral(NumericLiteralLeaf leaf)
        {
        }

        public void VisitStringLiteral(StringLiteralLeaf leaf)
        {
        }

        public void VisitWhile(WhileLeaf leaf)
        {
            leaf.Condition = ProcessReplacement(leaf.Condition, leaf);
            leaf.Body = ProcessReplacement(leaf.Body, leaf);
        }

        public void VisitBreak(BreakLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS, leaf);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS, leaf);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }
        
        private IASTLeaf ProcessReplacement(IASTLeaf leaf, IASTLeaf parent)
        {
            if (leaf == null) {
                return null;
            }
            
            leaf.Accept(_replacer);
            if (_replacer.ShouldReplace) {
                IASTLeaf newLeaf = _replacer.NewLeaf;
                
                if (_scopes != null) {
                    ScopeResolver resolver = new ScopeResolver(_scopes, _scopes.GetScope(parent));
                    newLeaf.Accept(resolver);
                }

                return newLeaf;
            }
            return leaf;
        }
    }
}