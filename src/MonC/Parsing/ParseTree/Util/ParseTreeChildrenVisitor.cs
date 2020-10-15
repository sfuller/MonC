using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Util
{
    public class ParseTreeChildrenVisitor : IParseTreeVisitor
    {
        private readonly ISyntaxTreeVisitor? _preOrderVisitor;
        private readonly ISyntaxTreeVisitor? _postOrderVisitor;
        private readonly ISyntaxTreeVisitor _childVisitor;

        public ParseTreeChildrenVisitor(ISyntaxTreeVisitor? preOrderVisitor, ISyntaxTreeVisitor? postOrderVisitor, ISyntaxTreeVisitor childVisitor)
        {
            _preOrderVisitor = preOrderVisitor;
            _postOrderVisitor = postOrderVisitor;
            _childVisitor = childVisitor;
        }

        private void VisitPreOrder(ISyntaxTreeNode node)
        {
            if (_preOrderVisitor != null) {
                node.AcceptSyntaxTreeVisitor(_preOrderVisitor);
            }
        }

        private void VisitPostOrder(ISyntaxTreeNode node)
        {
            if (_postOrderVisitor != null) {
                node.AcceptSyntaxTreeVisitor(_postOrderVisitor);
            }
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            VisitPreOrder(node);
            node.LHS.AcceptSyntaxTreeVisitor(_childVisitor);
            node.RHS.AcceptSyntaxTreeVisitor(_childVisitor);
            VisitPostOrder(node);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            VisitPreOrder(node);
            node.LHS.AcceptSyntaxTreeVisitor(_childVisitor);
            foreach (IExpressionNode argument in node.Arguments) {
                argument.AcceptSyntaxTreeVisitor(_childVisitor);
            }
            VisitPostOrder(node);
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node)
        {
            VisitPreOrder(node);
            VisitPostOrder(node);
        }

        public void VisitAccess(AccessParseNode node)
        {
            VisitPreOrder(node);
            node.Lhs.AcceptSyntaxTreeVisitor(_childVisitor);
            node.Rhs.AcceptSyntaxTreeVisitor(_childVisitor);
            VisitPostOrder(node);
        }
    }
}
