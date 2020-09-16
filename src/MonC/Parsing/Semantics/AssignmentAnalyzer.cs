using System.Collections.Generic;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Parsing.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Parsing.Semantics
{
    public class AssignmentAnalyzer :
            NoOpExpressionAndStatementVisitor, IParseTreeVisitor,
            IStatementReplacementVisitor, IExpressionReplacementVisitor
    {
        private readonly IList<(string message, ISyntaxTreeNode node)> _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;

        private bool _shouldReplace;
        private IExpressionNode _newExpressionNode;

        private readonly ScopeManager _scopeManager = new ScopeManager();

        // We don't reaplace any statement nodes, so we can keep this a constant, non-null value.
        private readonly IStatementNode _newStatementNode = new ExpressionStatementNode(new VoidExpressionNode());

        public AssignmentAnalyzer(IList<(string message, ISyntaxTreeNode node)> errors, IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;

            _newExpressionNode = new VoidExpressionNode();
        }

        public void Process(FunctionDefinitionNode function)
        {
            _scopeManager.ProcessFunction(function);

            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(this);
            ProcessStatementReplacementsVisitor statementReplacementsVisitor = new ProcessStatementReplacementsVisitor(this, this);
            ExpressionChildrenVisitor expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionReplacementsVisitor);
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(statementReplacementsVisitor, expressionChildrenVisitor);
            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void PrepareToVisit()
        {
            _shouldReplace = false;
        }

        bool IReplacementVisitor<IExpressionNode>.ShouldReplace => _shouldReplace;
        bool IReplacementVisitor<IStatementNode>.ShouldReplace => _shouldReplace;
        IExpressionNode IReplacementVisitor<IExpressionNode>.NewNode => _newExpressionNode;
        IStatementNode IReplacementVisitor<IStatementNode>.NewNode => _newStatementNode;

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

        public override void VisitUnknown(IBinaryOperationNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            if (!(node.LHS is IdentifierParseNode identifier)) {
                _errors.Add(("Expecting identifier", node.LHS));
                return;
            }

            _shouldReplace = true;
            IExpressionNode resultNode;

            DeclarationNode declaration = _scopeManager.GetScope(node).Variables.Find(d => d.Name == identifier.Name);
            if (declaration == null) {
                _errors.Add(($"Undeclared identifier {identifier.Name}", identifier));
                resultNode = new VoidExpressionNode();
            } else {
                resultNode = new AssignmentNode(declaration, node.RHS);
            }

            _newExpressionNode = resultNode;

            // TODO: Need more automated symbol association for new nodes.
            Symbol originalSymbol;
            _symbolMap.TryGetValue(node, out originalSymbol);
            _symbolMap[_newExpressionNode] = originalSymbol;
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
        }
    }
}
