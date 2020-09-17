using System;
using System.Collections.Generic;
using System.Linq;
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
    public class TranslateIdentifiersVisitor : NoOpExpressionAndStatementVisitor,
            IParseTreeVisitor, IExpressionReplacementVisitor, IStatementReplacementVisitor
    {
        private readonly Dictionary<string, FunctionDefinitionNode> _functions;
        private readonly EnumManager _enums;

        private readonly IList<(string name, ISyntaxTreeNode node)> _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;

        private readonly IStatementNode _newStatementNode;
        private IExpressionNode _newExpressionNode;

        private readonly ScopeManager _scopeManager = new ScopeManager();

        public TranslateIdentifiersVisitor(
            Dictionary<string, FunctionDefinitionNode> functions,
            IList<(string name, ISyntaxTreeNode node)> errors,
            EnumManager enums,
            IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _functions = functions;
            _enums = enums;
            _errors = errors;
            _symbolMap = symbolMap;

            _newExpressionNode = new VoidExpressionNode();
            _newStatementNode = new ExpressionStatementNode(new VoidExpressionNode());
        }

        public void PrepareToVisit()
        {
            ShouldReplace = false;
        }

        public bool ShouldReplace { get; private set; }

        IExpressionNode IReplacementVisitor<IExpressionNode>.NewNode => _newExpressionNode;
        IStatementNode IReplacementVisitor<IStatementNode>.NewNode => _newStatementNode;

        public void Process(FunctionDefinitionNode function)
        {
            _scopeManager.ProcessFunction(function);
            IExpressionReplacementVisitor expressionReplacementVisitor = new ScopedExpressionReplacementVisitor(this, _scopeManager);
            ProcessStatementReplacementsVisitor statementReplacementsVisitor = new ProcessStatementReplacementsVisitor(this, expressionReplacementVisitor);
            ProcessExpressionReplacementsVisitor expressionReplacementsVisitor = new ProcessExpressionReplacementsVisitor(expressionReplacementVisitor);
            ExpressionChildrenVisitor expressionChildrenVisitor = new ExpressionChildrenVisitor(expressionReplacementsVisitor);
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(statementReplacementsVisitor, expressionChildrenVisitor);
            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            ShouldReplace = true;

            DeclarationNode decl = _scopeManager.GetScope(node).Variables.Find(d => d.Name == node.Name);
            if (decl != null) {
                _newExpressionNode = UpdateSymbolMap(new VariableNode(decl), node);
                return;
            }

            EnumNode? enumNode = _enums.GetEnumeration(node.Name);
            if (enumNode != null) {
                _newExpressionNode = UpdateSymbolMap(new EnumValueNode(enumNode, node.Name), node);
                return;
            }

            ShouldReplace = false;
            _errors.Add(($"Undeclared identifier {node.Name}", node));
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            IdentifierParseNode? identifier = node.LHS as IdentifierParseNode;

            if (identifier == null) {
                _errors.Add(("LHS of function call operator is not an identifier.", node));
                return;
            }

            ShouldReplace = true;

            FunctionCallNode? resultNode = null;

            FunctionDefinitionNode function;
            if (!_functions.TryGetValue(identifier.Name, out function)) {
                _errors.Add(("Undefined function " + identifier.Name, node));
            } else if (function.Parameters.Length != node.ArgumentCount) {
                _errors.Add(($"Expected {function.Parameters.Length} argument(s), got {node.ArgumentCount}", node));
            } else {
                resultNode = new FunctionCallNode(function, node.GetArguments());
            }

            if (resultNode == null) {
                resultNode = MakeFakeFunctionCall(identifier, node);
            }

            UpdateSymbolMap(resultNode, node);
            _newExpressionNode = resultNode;
        }

        private FunctionCallNode MakeFakeFunctionCall(IdentifierParseNode identifier, FunctionCallParseNode call)
        {
            FunctionCallNode fakeFunctionCall = new FunctionCallNode(
                lhs: new FunctionDefinitionNode(
                    $"(placeholder) {identifier.Name}",
                    new TypeSpecifier("int", PointerType.NotAPointer),
                    Array.Empty<DeclarationNode>(),
                    new BodyNode(),
                    isExported: false
                ),
                arguments: Enumerable.Range(0, call.ArgumentCount).Select(call.GetArgument));

            return fakeFunctionCall;
        }

        private IExpressionNode UpdateSymbolMap(IExpressionNode node, IExpressionNode original)
        {
            Symbol originalSymbol;
            _symbolMap.TryGetValue(original, out originalSymbol);
            _symbolMap[node] = originalSymbol;
            return node;
        }
    }
}
