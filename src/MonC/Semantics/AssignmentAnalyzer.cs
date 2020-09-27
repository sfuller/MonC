using System.Collections.Generic;
using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Semantics.Scoping;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Semantics
{
    public class AssignmentAnalyzer : IReplacementSource, IVisitor<IBinaryOperationNode>, IVisitor<AssignmentParseNode>
    {
        private readonly IErrorManager _errors;
        private readonly IDictionary<ISyntaxTreeNode, Symbol> _symbolMap;

        private readonly ScopeManager _scopeManager = new ScopeManager();
        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();
        private readonly ParseTreeDelegator _parseTreeDelegator = new ParseTreeDelegator();

        private bool _shouldReplace;
        private ISyntaxTreeNode _newNode = new VoidExpressionNode();

        public AssignmentAnalyzer(IErrorManager errors, IDictionary<ISyntaxTreeNode, Symbol> symbolMap)
        {
            _errors = errors;
            _symbolMap = symbolMap;

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

    }
}
