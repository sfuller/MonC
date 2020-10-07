using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree.Nodes;

namespace MonC.Parsing.ParseTree.Util
{
    public class ParseTreeChildrenVisitor : IParseTreeVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;
        private readonly ISyntaxTreeVisitor _childVisitor;

        public ParseTreeChildrenVisitor(ISyntaxTreeVisitor visitor, ISyntaxTreeVisitor childVisitor)
        {
            _visitor = visitor;
            _childVisitor = childVisitor;
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
            node.LHS.AcceptSyntaxTreeVisitor(_childVisitor);
            node.RHS.AcceptSyntaxTreeVisitor(_childVisitor);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
            node.LHS.AcceptSyntaxTreeVisitor(_childVisitor);
            foreach (IExpressionNode argument in node.Arguments) {
                argument.AcceptSyntaxTreeVisitor(_childVisitor);
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
        }

        public void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
        }

        public void VisitAccess(AccessParseNode node)
        {
            node.AcceptSyntaxTreeVisitor(_visitor);
            node.Lhs.AcceptSyntaxTreeVisitor(_childVisitor);
            node.Rhs.AcceptSyntaxTreeVisitor(_childVisitor);
        }
    }
}
