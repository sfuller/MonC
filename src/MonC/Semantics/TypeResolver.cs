using MonC.Parsing.ParseTree;
using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Util.Delegators;
using MonC.SyntaxTree.Util.ReplacementVisitors;
using MonC.TypeSystem;
using MonC.TypeSystem.Types;

namespace MonC.Semantics
{
    public class TypeResolver : ISpecifierVisitor, IParseTreeVisitor, IReplacementSource
    {
        private readonly TypeManager _typeManager;
        private readonly IErrorManager _errors;

        private readonly SyntaxTreeDelegator _replacementDelegator = new SyntaxTreeDelegator();

        public TypeResolver(TypeManager typeManager, IErrorManager errors)
        {
            _typeManager = typeManager;
            _errors = errors;

            _replacementDelegator.SpecifierVisitor = this;
            NewNode = new VoidExpressionNode();
        }

        public void Process(FunctionDefinitionNode function)
        {
            ProcessReplacementsVisitorChain replacementsVisitorChain = new ProcessReplacementsVisitorChain(this);
            replacementsVisitorChain.ProcessReplacements(function);
        }

        public ISyntaxTreeVisitor ReplacementVisitor => _replacementDelegator;
        public bool ShouldReplace { get; private set; }
        public ISyntaxTreeNode NewNode { get; private set; }

        public void PrepareToVisit()
        {
            ShouldReplace = false;
        }


        public void VisitTypeSpecifier(TypeSpecifierNode node)
        {
        }

        public void VisitUnknown(ISpecifierNode node)
        {
            if (node is IParseTreeNode parseNode) {
                parseNode.AcceptParseTreeVisitor(this);
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            IType? type = _typeManager.GetType(node.Name, node.PointerMode);
            if (type == null) {
                _errors.AddError($"Undefined type name \"{node.Name}\"", node);
                return;
            }

            NewNode = new TypeSpecifierNode(type);
            ShouldReplace = true;
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
        }
    }
}
