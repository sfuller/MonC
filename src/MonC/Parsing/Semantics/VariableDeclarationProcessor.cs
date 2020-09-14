using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Statements;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Parsing.Semantics
{
    // TODO: Rename to DuplicateDeclarationAnalyzer?
    public class VariableDeclarationProcessor : NoOpExpressionAndStatementVisitor, IScopeHandler
    {
        private readonly IList<(string message, ISyntaxTreeLeaf leaf)> _errors;

        public VariableDeclarationProcessor(IList<(string message, ISyntaxTreeLeaf leaf)> errors)
        {
            _errors = errors;
        }

        public Scope CurrentScope { private get; set; }

        public void Process(FunctionDefinitionLeaf function)
        {
            WalkScopeVisitor scopeVisitor = new WalkScopeVisitor(this, this, this, Scope.New(function));
            function.Body.AcceptStatements(scopeVisitor);
        }

        public override void VisitDeclaration(DeclarationLeaf leaf)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            DeclarationLeaf previousLeaf = CurrentScope.Variables.Find(existingLeaf => leaf.Name == existingLeaf.Name);

            if (previousLeaf != null) {
                _errors.Add(($"Duplicate declaration {leaf.Name}", leaf));
            }
        }

        public override void VisitUnknown(IExpressionLeaf leaf)
        {
            // It is okay to not handle parse tree leaves, or any other unrecognized leaves for that matter.
        }
    }
}
