using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.Parsing.ParseTree.Util;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.NoOpVisitors;
using MonC.SyntaxTree.Util.ReplacementVisitors;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Semantics
{
    public class TranslateAccessVisitor : NoOpExpressionVisitor, IReplacementSource, IVisitor<AccessParseNode>
    {
        private readonly IErrorManager _errors;
        private readonly ExpressionTypeManager _expressionTypeManager;

        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();
        private readonly ParseTreeDelegator _parseTreeDelegator = new ParseTreeDelegator();

        public TranslateAccessVisitor(IErrorManager errors, ExpressionTypeManager expressionTypeManager)
        {
            _errors = errors;
            _expressionTypeManager = expressionTypeManager;

            NewNode = new VoidExpressionNode();

            _replacementDelegator.ExpressionVisitor = this;
            _parseTreeDelegator.AccessVisitor = this;
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
            ProcessReplacementsVisitorChain visitorChain = new ProcessReplacementsVisitorChain(this, isPostOrder: true);
            ParseTreeChildrenVisitor parseTreeChildrenVisitor
                = new ParseTreeChildrenVisitor(null, visitorChain.ReplacementVisitor, visitorChain.ChildrenVisitor);
            ProcessParseTreeReplacementsVisitor parseTreeReplacementsVisitor
                = new ProcessParseTreeReplacementsVisitor(this);
            visitorChain.ExpressionChildrenVisitor.ExtensionChildrenVisitor = new ParseTreeVisitorExtension(parseTreeChildrenVisitor);
            visitorChain.ExpressionReplacementsVisitor.ExtensionVisitor = new ParseTreeVisitorExtension(parseTreeReplacementsVisitor);
            visitorChain.ProcessReplacements(function);
        }

        public override void VisitUnknown(IExpressionNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(_parseTreeDelegator);
            }
        }

        public void Visit(AccessParseNode node)
        {
            IType type = _expressionTypeManager.GetExpressionType(node.Lhs);

            if (!(type is StructType structType)) {
                _errors.AddError($"Type '{type.Represent()}' has no accessible members.", node);
                return;
            }

            DeclarationNode? declaration = structType.Struct.Members.Find(decl => decl.Name == node.Rhs.Name);
            if (declaration == null) {
                _errors.AddError($"No such member {node.Rhs.Name} in struct {structType.Struct.Name}.", node.Rhs);
                return;
            }

            NewNode = new AccessNode(node.Lhs, declaration);
            ShouldReplace = true;
        }
    }
}
