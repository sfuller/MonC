using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.NoOpVisitors;

namespace MonC.Semantics
{
    // TODO: Rename to DuplicateDeclarationAnalyzer?
    public class VariableDeclarationProcessor : NoOpStatementVisitor, ISyntaxTreeVisitor, IScopeHandler
    {
        private readonly IErrorManager _errors;

        public VariableDeclarationProcessor(IErrorManager errors)
        {
            _errors = errors;
        }

        public Scope CurrentScope { private get; set; }

        public void Process(FunctionDefinitionNode function)
        {
            WalkScopeVisitor scopeVisitor = new WalkScopeVisitor(this, this, Scope.New(function));
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

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
        }

        public void VisitStatement(IStatementNode node)
        {
            node.AcceptStatementVisitor(this);
        }

        public void VisitExpression(IExpressionNode node)
        {
        }

        public void VisitSpecifier(ISpecifierNode node)
        {
        }
    }
}
