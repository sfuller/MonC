using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Parsing.ParseTree.Util;
using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics
{
    public class AssignmentAnalyzer : IReplacementSource, IVisitor<IBinaryOperationNode>, IVisitor<AssignmentParseNode>
    {
        private readonly IErrorManager _errors;
        private readonly SemanticContext _semanticModule;

        private readonly ScopeManager _scopeManager = new ScopeManager();
        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();
        private readonly ParseTreeDelegator _parseTreeDelegator = new ParseTreeDelegator();

        private bool _shouldReplace;
        private ISyntaxTreeNode _newNode = new VoidExpressionNode();

        public AssignmentAnalyzer(IErrorManager errors, SemanticContext semanticModule)
        {
            _errors = errors;
            _semanticModule = semanticModule;

            ExpressionDelegator expressionDelegator = new ExpressionDelegator();
            BinaryOperationDelegator binOpDelegator = new BinaryOperationDelegator();

            _replacementDelegator.ExpressionVisitor = expressionDelegator;
            expressionDelegator.BinaryOperationVisitor = binOpDelegator;
            binOpDelegator.UnknownVisitor = this;

            _parseTreeDelegator.AssignmentVisitor = this;
        }

        public void Process(FunctionDefinitionNode function)
        {
            _scopeManager.ProcessFunction(function);

            ProcessReplacementsVisitorChain replacementsVisitorChain = new ProcessReplacementsVisitorChain(this);
            replacementsVisitorChain.ProcessReplacements(function);
        }

        public void PrepareToVisit()
        {
            _shouldReplace = false;
        }

        ISyntaxTreeVisitor IReplacementSource.ReplacementVisitor => _replacementDelegator;
        bool IReplacementSource.ShouldReplace => _shouldReplace;
        ISyntaxTreeNode IReplacementSource.NewNode => _newNode;

        public void Visit(IBinaryOperationNode node)
        {
            if (node is IParseTreeNode parseTreeNode) {
                parseTreeNode.AcceptParseTreeVisitor(_parseTreeDelegator);
            }
        }

        public void Visit(AssignmentParseNode node)
        {
            IAddressableNode? assignableNode = node.LHS as IAddressableNode;
            if (assignableNode == null || !assignableNode.IsAddressable()) {
                _errors.AddError("Left hand side of assignment is not assignable.", node);
                return;
            }

            _shouldReplace = true;
            _newNode = new AssignmentNode(assignableNode, node.RHS);

            // TODO: Need more automated symbol association for new nodes.
            Symbol originalSymbol;
            _semanticModule.SymbolMap.TryGetValue(node, out originalSymbol);
            _semanticModule.SymbolMap[_newNode] = originalSymbol;
        }

    }
}
