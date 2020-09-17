using System.Collections.Generic;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Parsing.Semantics
{
    // TODO: Rename to DuplicateDeclarationAnalyzer?
    public class VariableDeclarationProcessor : NoOpExpressionAndStatementVisitor, IScopeHandler
    {
        private readonly IList<(string message, ISyntaxTreeNode node)> _errors;

        public VariableDeclarationProcessor(IList<(string message, ISyntaxTreeNode node)> errors)
        {
            _errors = errors;
        }

        public Scope CurrentScope { private get; set; }

        public void Process(FunctionDefinitionNode function)
        {
            WalkScopeVisitor scopeVisitor = new WalkScopeVisitor(this, this, this, Scope.New(function));
            function.Body.VisitStatements(scopeVisitor);
        }

        public override void VisitDeclaration(DeclarationNode node)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            DeclarationNode previousNode = CurrentScope.Variables.Find(existingNode => node.Name == existingNode.Name);

            if (previousNode != null) {
                _errors.Add(($"Duplicate declaration {node.Name}", node));
            }
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            // It is okay to not handle parse tree nodes, or any other unrecognized nodes for that matter.
        }
    }
}
