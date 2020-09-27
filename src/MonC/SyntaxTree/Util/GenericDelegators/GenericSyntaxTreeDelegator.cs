using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.SyntaxTree.Util.GenericDelegators
{
    public class GenericSyntaxTreeDelegator : ISyntaxTreeVisitor
    {
        private readonly IVisitor<ISyntaxTreeNode> _visitor;

        public GenericSyntaxTreeDelegator(IVisitor<ISyntaxTreeNode> visitor)
        {
            _visitor = visitor;
        }

        public void VisitTopLevelStatement(ITopLevelStatementNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitStatement(IStatementNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitExpression(IExpressionNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitSpecifier(ISpecifierNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitEnumDeclaration(EnumDeclarationNode node)
        {
            _visitor.Visit(node);
        }

        public void VisitUnknown(ISyntaxTreeNode node)
        {
            _visitor.Visit(node);
        }
    }
}
