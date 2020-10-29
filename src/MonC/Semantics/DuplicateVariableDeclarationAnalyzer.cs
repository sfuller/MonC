using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.Semantics
{
    public class DuplicateVariableDeclarationAnalyzer : IVisitor<DeclarationNode>
    {
        private readonly IErrorManager _errors;
        private readonly ScopeManager _scopes;

        public DuplicateVariableDeclarationAnalyzer(IErrorManager errors, ScopeManager scopes)
        {
            _errors = errors;
            _scopes = scopes;
        }

        public void Process(FunctionDefinitionNode function)
        {
            SyntaxTreeDelegator visitor = new SyntaxTreeDelegator();
            StatementDelegator statementDelegator = new StatementDelegator();
            statementDelegator.DeclarationVisitor = this;
            visitor.StatementVisitor = statementDelegator;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(visitor, childrenVisitor);
            childrenVisitor.StatementVisitor = statementChildrenVisitor;

            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void Visit(DeclarationNode node)
        {
            // Ensure declaration doesn't duplicate another declaration in the current scope.
            Scope scope = _scopes.GetScope(node).Scope;
            DeclarationNode previousNode = scope.Variables.Find(existingNode => node.Name == existingNode.Name && node != existingNode);

            if (previousNode != null) {
                _errors.AddError($"Duplicate declaration {node.Name}", node);
            }
        }
    }
}
