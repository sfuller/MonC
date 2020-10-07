using System;
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

        private readonly ScopeManager _scopeManager = new ScopeManager();
        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();

        private readonly ParseTreeDelegator _parseTreeReplacementDelegator = new ParseTreeDelegator();

        public TranslateIdentifiersVisitor(SemanticContext semanticModule, IErrorManager errors)
        {
            _semanticModule = semanticModule;
            _errors = errors;

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

        public void Process(FunctionDefinitionNode function)
        {
            _scopeManager.ProcessFunction(function);

            // TODO: Helper class to reduce repeating of this setup code.
            ProcessReplacementsVisitorChain visitorChain = new ProcessReplacementsVisitorChain(this);
            ParseTreeChildrenVisitor parseTreeChildrenVisitor
                = new ParseTreeChildrenVisitor(visitorChain.ReplacementVisitor, visitorChain.ChildrenVisitor);
            ProcessParseTreeReplacementsVisitor parseTreeReplacementsVisitor
                = new ProcessParseTreeReplacementsVisitor(this);
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

            DeclarationNode decl = _scopeManager.GetScope(node).Variables.Find(d => d.Name == node.Name);
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
            } else {
                resultNode = new FunctionCallNode(function, node.Arguments);
            }

            if (resultNode == null) {
                resultNode = MakeFakeFunctionCall(identifier, node);
            }

            UpdateSymbolMap(resultNode, node);
            NewNode = resultNode;
        }

        private FunctionCallNode MakeFakeFunctionCall(IdentifierParseNode identifier, FunctionCallParseNode call)
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
                arguments: call.Arguments);

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
