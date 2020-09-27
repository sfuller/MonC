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

        public void VisitAssignment(AssignmentParseNode node)
        {
            if (AssignmentVisitor != null) {
                AssignmentVisitor.Visit(node);
            }
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
            if (IdentifierVisitor != null) {
                IdentifierVisitor.Visit(node);
            }
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            if (FunctionCallVisitor != null) {
                FunctionCallVisitor.Visit(node);
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
            if (TypeSpecifierVisitor != null) {
                TypeSpecifierVisitor.Visit(node);
            }
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
            if (StructFunctionAssociationVisitor != null) {
                StructFunctionAssociationVisitor.Visit(node);
            }
        }
    }
}
