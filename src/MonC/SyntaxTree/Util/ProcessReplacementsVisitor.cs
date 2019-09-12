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
            leaf.RHS = ProcessReplacement(leaf.RHS, leaf.LHS);
        }

        public void VisitUnaryOperation(UnaryOperationLeaf leaf)
        {
            leaf.RHS = ProcessReplacement(leaf.RHS, leaf);
        }

        public void VisitBody(BodyLeaf leaf)
        {
            IASTLeaf previousLeaf = leaf;
            for (int i = 0, ilen = leaf.Length; i < ilen; ++i) {
                IASTLeaf statementLeaf = leaf.GetStatement(i);
                leaf.SetStatement(i, ProcessReplacement(statementLeaf, previousLeaf));
                previousLeaf = statementLeaf;
            }
        }

        public void VisitDeclaration(DeclarationLeaf leaf)
        {
            IASTLeaf assignment;
            if (leaf.Assignment.Get(out assignment)) {
                leaf.Assignment = new Optional<IASTLeaf>(ProcessReplacement(assignment, leaf));    
            }
            
        }

        public void VisitFor(ForLeaf leaf)
        {
            leaf.Declaration = ProcessReplacement(leaf.Declaration, leaf);
            leaf.Condition = ProcessReplacement(leaf.Condition, leaf.Declaration);
            leaf.Update = ProcessReplacement(leaf.Update, leaf.Condition);
            leaf.Body = ProcessReplacement(leaf.Body, leaf.Update);
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
        
        private T ProcessReplacement<T>(T leaf, IASTLeaf previous) where T : class, IASTLeaf
        {
            Optional<T> result = ProcessReplacement(new Optional<T>(leaf), previous);
            T nonNullResult;
            if (result.Get(out nonNullResult)) {
                return nonNullResult;
            }
            throw new InvalidOperationException("Result of ProcessReplacement cannot be null here");
        }

        private Optional<T> ProcessReplacement<T>(Optional<T> leaf, IASTLeaf previous) where T : class, IASTLeaf
        {
            T nonNullLeaf;
            if (leaf.Get(out nonNullLeaf)) {
                nonNullLeaf.Accept(_replacer);    
            }
            
            if (_replacer.ShouldReplace) {
                IASTLeaf newLeaf = _replacer.NewLeaf;
                
                if (_scopes != null) {
                    ScopeResolver resolver = new ScopeResolver(_scopes, _scopes.GetScope(nonNullLeaf));
                    newLeaf.Accept(resolver);
                }

                if (newLeaf == null) {
                    return new Optional<T>();
                }
                
                // TODO: This sucks, considering making separate visitors for different types.
                T newTypedLeaf = newLeaf as T;

                if (newTypedLeaf == null) {
                    return new Optional<T>();
                }
                
                return new Optional<T>(newTypedLeaf);
            }
            return leaf;
        }
    }
}