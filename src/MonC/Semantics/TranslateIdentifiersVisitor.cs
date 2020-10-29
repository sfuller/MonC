using System;
using System.Linq;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Parsing.ParseTree.Util;
using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;
using MonC.TypeSystem;

namespace MonC.Semantics
{
    public class TranslateIdentifiersVisitor : NoOpExpressionVisitor, IReplacementSource,
            IVisitor<IdentifierParseNode>, IVisitor<FunctionCallParseNode>
    {
        private readonly SemanticContext _semanticModule;
        private readonly IErrorManager _errors;

        private readonly ScopeManager _scopeManager;
        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();

        private readonly ParseTreeDelegator _parseTreeReplacementDelegator = new ParseTreeDelegator();

        public TranslateIdentifiersVisitor(SemanticContext semanticModule, IErrorManager errors, ScopeManager scopes)
        {
            _semanticModule = semanticModule;
            _errors = errors;
            _scopeManager = scopes;

            NewNode = new VoidExpressionNode();

            _replacementDelegator.ExpressionVisitor = this;
            _parseTreeReplacementDelegator.IdentifierVisitor = this;
            _parseTreeReplacementDelegator.FunctionCallVisitor = this;
        }

        public void PrepareToVisit()
        {
            ShouldReplace = false;
        }

        public ISyntaxTreeVisitor ReplacementVisitor => _replacementDelegator;
        public bool ShouldReplace { get; private set; }
        public ISyntaxTreeNode NewNode { get; private set; }

        public void Process(FunctionDefinitionNode function, IReplacementListener listener)
        {
            // TODO: Helper class to reduce repeating of this setup code.
            ProcessReplacementsVisitorChain visitorChain = new ProcessReplacementsVisitorChain(this, listener);
            ParseTreeChildrenVisitor parseTreeChildrenVisitor
                = new ParseTreeChildrenVisitor(visitorChain.ReplacementVisitor, null, visitorChain.ChildrenVisitor);
            ProcessParseTreeReplacementsVisitor parseTreeReplacementsVisitor
                = new ProcessParseTreeReplacementsVisitor(this, listener);
            visitorChain.ExpressionChildrenVisitor.ExtensionChildrenVisitor = new ParseTreeVisitorExtension(parseTreeChildrenVisitor);
            visitorChain.ExpressionReplacementsVisitor.ExtensionVisitor = new ParseTreeVisitorExtension(parseTreeReplacementsVisitor);

            visitorChain.ProcessReplacements(function);
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(_parseTreeReplacementDelegator);
            }
        }

        public void Visit(IdentifierParseNode node)
        {
            ShouldReplace = true;

            NodeScopeInfo nodeScopeInfo = _scopeManager.GetScope(node);
            DeclarationNode? decl = nodeScopeInfo.Scope.FindNearestDeclaration(node.Name, nodeScopeInfo.DeclarationIndex);
            if (decl != null) {
                NewNode = UpdateSymbolMap(new VariableNode(decl), node);
                return;
            }

            if (_semanticModule.EnumInfo.TryGetValue(node.Name, out EnumDeclarationInfo identifierInfo)) {
                NewNode = UpdateSymbolMap(new EnumValueNode(identifierInfo.Declaration), node);
                return;
            }

            ShouldReplace = false;
            _errors.AddError($"Undeclared identifier {node.Name}", node);
        }

        public void Visit(FunctionCallParseNode node)
        {
            IdentifierParseNode? identifier = node.LHS as IdentifierParseNode;

            if (identifier == null) {
                _errors.AddError("LHS of function call operator is not an identifier.", node);
                return;
            }

            ShouldReplace = true;

            FunctionCallNode? resultNode = null;

            if (!_semanticModule.Functions.TryGetValue(identifier.Name, out FunctionDefinitionNode function)) {
                _errors.AddError("Undefined function " + identifier.Name, node);
            } else if (function.Parameters.Length != node.Arguments.Count) {
                _errors.AddError($"Expected {function.Parameters.Length} argument(s), got {node.Arguments.Count}", node);
                return;
            } else {
                resultNode = new FunctionCallNode(function, node.Arguments);
            }

            if (resultNode == null) {
                resultNode = MakeFakeFunctionCall(identifier);
            }

            UpdateSymbolMap(resultNode, node);
            NewNode = resultNode;
        }

        private FunctionCallNode MakeFakeFunctionCall(IdentifierParseNode identifier)
        {
            FunctionCallNode fakeFunctionCall = new FunctionCallNode(
                lhs: new FunctionDefinitionNode(
                    $"(placeholder) {identifier.Name}",
                    new TypeSpecifierParseNode("int", PointerMode.NotAPointer),
                    Array.Empty<DeclarationNode>(),
                    new BodyNode(),
                    isExported: false,
                    isDrop: false
                ),
                arguments: Enumerable.Empty<IExpressionNode>());

            return fakeFunctionCall;
        }

        private IExpressionNode UpdateSymbolMap(IExpressionNode node, IExpressionNode original)
        {
            Symbol originalSymbol;
            _semanticModule.SymbolMap.TryGetValue(original, out originalSymbol);
            _semanticModule.SymbolMap[node] = originalSymbol;
            return node;
        }
    }
}
