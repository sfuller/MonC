using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree.Util;

namespace MonC.Parsing.ParseTree.Util
{
    public class ParseTreeDelegator : IParseTreeVisitor
    {
        public IVisitor<AssignmentParseNode>? AssignmentVisitor;
        public IVisitor<IdentifierParseNode>? IdentifierVisitor;
        public IVisitor<FunctionCallParseNode>? FunctionCallVisitor;
        public IVisitor<TypeSpecifierParseNode>? TypeSpecifierVisitor;
        public IVisitor<StructFunctionAssociationParseNode>? StructFunctionAssociationVisitor;
        public IVisitor<DeclarationIdentifierParseNode>? DeclarationIdentifierVisitor;
        public IVisitor<AccessParseNode>? AccessVisitor;

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
            DeclarationIdentifierVisitor?.Visit(node);
        }

        public void VisitAccess(AccessParseNode node)
        {
            AccessVisitor?.Visit(node);
        }
    }
}
