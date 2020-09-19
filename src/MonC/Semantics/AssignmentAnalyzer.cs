using System.Collections.Generic;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics
{
    public class AssignmentAnalyzer : NoOpExpressionVisitor, IParseTreeVisitor, IReplacementSource
    {
        private readonly IErrorManager _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;

        private readonly ScopeManager _scopeManager = new ScopeManager();
        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();

        private bool _shouldReplace;
        private ISyntaxTreeNode _newNode = new VoidExpressionNode();

        public AssignmentAnalyzer(IErrorManager errors, IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;

            _replacementDelegator.ExpressionVisitor = this;
        }

        public void Process(FunctionDefinitionNode function)
        {
            _scopeManager.ProcessFunction(function);

            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(this);

            // Configure the expression children visitor to use the expression replacements visitor for expressions.
            SyntaxTreeDelegator expressionChildrenDelegator = new SyntaxTreeDelegator();
            expressionChildrenDelegator.ExpressionVisitor = expressionReplacementsVisitor;
            ExpressionChildrenVisitor expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionChildrenDelegator);

            // Configure the statement children visitor to use the expression children visitor when encountering expressions.
            SyntaxTreeDelegator statementChildrenDelegator = new SyntaxTreeDelegator();
            statementChildrenDelegator.ExpressionVisitor = expressionChildrenVisitor;
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(statementChildrenDelegator);

            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void PrepareToVisit()
        {
            _shouldReplace = false;
        }

        ISyntaxTreeVisitor IReplacementSource.ReplacementVisitor => _replacementDelegator;
        bool IReplacementSource.ShouldReplace => _shouldReplace;
        ISyntaxTreeNode IReplacementSource.NewNode => _newNode;

        public override void VisitBinaryOperation(IBinaryOperationNode node)
        {
            node.AcceptBinaryOperationVisitor(this);
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            if (!(node.LHS is IdentifierParseNode identifier)) {
                _errors.AddError("Expecting identifier", node.LHS);
                return;
            }

            _shouldReplace = true;
            IExpressionNode resultNode;

            DeclarationNode declaration = _scopeManager.GetScope(node).Variables.Find(d => d.Name == identifier.Name);
            if (declaration == null) {
                _errors.AddError($"Undeclared identifier {identifier.Name}", identifier);
                resultNode = new VoidExpressionNode();
            } else {
                resultNode = new AssignmentNode(declaration, node.RHS);
            }

            _newNode = resultNode;

            // TODO: Need more automated symbol association for new nodes.
            Symbol originalSymbol;
            _symbolMap.TryGetValue(node, out originalSymbol);
            _symbolMap[_newNode] = originalSymbol;
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
        }
    }
}
