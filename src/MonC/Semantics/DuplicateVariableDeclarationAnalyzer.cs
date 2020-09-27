using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Semantics
{
    public class DuplicateVariableDeclarationAnalyzer : NoOpStatementVisitor, IScopeHandler
    {
        private readonly IErrorManager _errors;

        private readonly SyntaxTreeDelegator _delegator = new SyntaxTreeDelegator();

        public DuplicateVariableDeclarationAnalyzer(IErrorManager errors)
        {
            _errors = errors;

            _delegator.StatementVisitor = this;
        }

        public Scope CurrentScope { private get; set; }

        public void Process(FunctionDefinitionNode function)
        {
            WalkScopeVisitor scopeVisitor = new WalkScopeVisitor(this, _delegator, Scope.New(function));
            function.Body.VisitStatements(scopeVisitor);
        }

        public override void VisitDeclaration(DeclarationNode node)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            DeclarationNode previousNode = CurrentScope.Variables.Find(existingNode => node.Name == existingNode.Name);

            if (previousNode != null) {
                _errors.AddError($"Duplicate declaration {node.Name}", node);
            }
        }
    }
}
