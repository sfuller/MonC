using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.ParseTree
{
    public class ParseTreeDelegator : IParseTreeVisitor
    {
        public IVisitor<AssignmentParseNode>? AssignmentVisitor;
        public IVisitor<IdentifierParseNode>? IdentifierVisitor;
        public IVisitor<FunctionCallParseNode>? FunctionCallVisitor;
        public IVisitor<TypeSpecifierParseNode>? TypeSpecifierVisitor;
        public IVisitor<StructFunctionAssociationParseNode>? StructFunctionAssociationVisitor;
        public IVisitor<DeclarationIdentifierParseNode>? DeclarationIdentifierParseNodeVisitor;
        public IVisitor<AccessParseNode>? AccessParseNodeVisitor;

        public void VisitAssignment(AssignmentParseNode node)
        {
            AssignmentVisitor?.Visit(node);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            IdentifierVisitor?.Visit(node);
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            FunctionCallVisitor?.Visit(node);
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            TypeSpecifierVisitor?.Visit(node);
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            StructFunctionAssociationVisitor?.Visit(node);
        }

        public void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node)
        {
            DeclarationIdentifierParseNodeVisitor?.Visit(node);
        }

        public void VisitAccess(AccessParseNode node)
        {
            AccessParseNodeVisitor?.Visit(node);
        }
    }
}
