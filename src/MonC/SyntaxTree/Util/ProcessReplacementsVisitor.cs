using System;
using MonC.Parsing.Scoping;

namespace MonC.SyntaxTree.Util
{
    public class ProcessReplacementsVisitor : IASTLeafVisitor
    {
        private readonly ScopeCache? _scopes;
        private IReplacementVisitor _replacer;

        public ProcessReplacementsVisitor(IReplacementVisitor replacer, ScopeCache? scopes = null)
        {
            _scopes = scopes;
            _replacer = replacer;
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

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IASTLeaf statementLeaf = leaf.GetStatement(i);
                leaf.SetStatement(i, ProcessReplacement(statementLeaf));
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            leaf.Assignment = ProcessOptionalReplacement(leaf.Assignment);
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration = ProcessReplacement(leaf.Declaration);
            leaf.Condition = ProcessReplacement(leaf.Condition);
            leaf.Update = ProcessReplacement(leaf.Update);
            leaf.Body = ProcessReplacement(leaf.Body);
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
            leaf.ElseBody = ProcessOptionalReplacement(leaf.ElseBody);
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

        public void VisitContinue(ContinueLeaf leaf)
        {
        }

        public void VisitReturn(ReturnLeaf leaf)
        {
            leaf.RHS = ProcessOptionalReplacement(leaf.RHS);
        }

        public void VisitAssignment(AssignmentLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS);
        }

        public void VisitEnum(EnumLeaf leaf)
        {
        }

        public void VisitEnumValue(EnumValueLeaf leaf)
        {
        }
        
        private T? ProcessOptionalReplacement<T>(T? leaf) where T : class, IASTLeaf
        {
            if (leaf == null) {
                return null;
            }
            return ProcessPossiblyNullReplacement(leaf);
        }

        private T ProcessReplacement<T>(T leaf) where T : class, IASTLeaf
        {
            T? result = ProcessPossiblyNullReplacement(leaf);
            if (result == null) {
                throw new InvalidOperationException("Result of ProcessReplacement cannot be null here");                
            }
            return result;
        }

        private T? ProcessPossiblyNullReplacement<T>(T leaf) where T : class, IASTLeaf
        {
            leaf.Accept(_replacer);

            if (!_replacer.ShouldReplace) {
                return leaf;
            }
            
            IASTLeaf? newLeaf = _replacer.NewLeaf;
            
            if (newLeaf == null) {
                return null;
            }
            
            if (_scopes != null) {
                ScopeResolver resolver = new ScopeResolver(_scopes, _scopes.GetScope(leaf));
                newLeaf.Accept(resolver);
            }
            
            // TODO: This sucks, considering making separate visitors for different types.
            try {
                return (T) newLeaf;
            } catch (InvalidCastException e) {
                throw new InvalidCastException("Replacement visitor returned incorrect type for this field.", e);
            }
        }
    }
}