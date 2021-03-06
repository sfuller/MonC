using MonC.Parsing.ParseTree.Nodes;

namespace MonC.Parsing.ParseTree
{
    public interface IParseTreeVisitor
    {
        void VisitAssignment(AssignmentParseNode node);
        void VisitIdentifier(IdentifierParseNode node);
        void VisitFunctionCall(FunctionCallParseNode node);
        void VisitTypeSpecifier(TypeSpecifierParseNode node);
        void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node);
        void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node);
        void VisitAccess(AccessParseNode node);
    }
}
