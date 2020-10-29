using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree.Util.ReplacementVisitors;

namespace MonC.Parsing.ParseTree.Util
{
    public class ProcessParseTreeReplacementsVisitor : IParseTreeVisitor
    {
        public readonly ReplacementProcessor _processor;

        public ProcessParseTreeReplacementsVisitor(IReplacementSource replacementSource, IReplacementListener listener)
        {
            _processor = new ReplacementProcessor(replacementSource, listener);
        }

        public void VisitAssignment(AssignmentParseNode node)
        {
            node.LHS = _processor.ProcessReplacement(node.LHS);
            node.RHS = _processor.ProcessReplacement(node.RHS);
        }

        public void VisitIdentifier(IdentifierParseNode node)
        {
        }

        public void VisitFunctionCall(FunctionCallParseNode node)
        {
            node.LHS = _processor.ProcessReplacement(node.LHS);
            for (int i = 0, ilen = node.Arguments.Count; i < ilen; ++i) {
                node.Arguments[i] = _processor.ProcessReplacement(node);
            }
        }

        public void VisitTypeSpecifier(TypeSpecifierParseNode node)
        {
        }

        public void VisitStructFunctionAssociation(StructFunctionAssociationParseNode node)
        {
        }

        public void VisitDeclarationIdentifier(DeclarationIdentifierParseNode node)
        {
        }

        public void VisitAccess(AccessParseNode node)
        {
            node.Lhs = _processor.ProcessReplacement(node.Lhs);
            node.Rhs = _processor.ProcessReplacement(node.Rhs);
        }
    }
}
