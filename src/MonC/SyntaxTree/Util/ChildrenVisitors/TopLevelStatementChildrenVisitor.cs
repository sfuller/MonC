using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Statements;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class TopLevelStatementChildrenVisitor : ITopLevelStatementVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;
        private readonly ISyntaxTreeVisitor _childrenVisitor;

        public TopLevelStatementChildrenVisitor(ISyntaxTreeVisitor visitor, ISyntaxTreeVisitor childrenVisitor)
        {
            _visitor = visitor;
            _childrenVisitor = childrenVisitor;
        }

        public void VisitEnum(EnumNode node)
        {
            _visitor.VisitTopLevelStatement(node);
            foreach (EnumDeclarationNode declaration in node.Declarations) {
                declaration.AcceptSyntaxTreeVisitor(_childrenVisitor);
            }
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            _visitor.VisitTopLevelStatement(node);
            node.ReturnType.AcceptSyntaxTreeVisitor(_childrenVisitor);
            for (int i = 0, ilen = node.Parameters.Length; i < ilen; ++i) {
                node.Parameters[i].AcceptSyntaxTreeVisitor(_childrenVisitor);
            }
            node.Body.AcceptSyntaxTreeVisitor(_childrenVisitor);
        }

        public void VisitStruct(StructNode node)
        {
            _visitor.VisitTopLevelStatement(node);
            foreach (IStructFunctionAssociationNode functionAssociationNode in node.FunctionAssociations) {
                functionAssociationNode.AcceptSyntaxTreeVisitor(_childrenVisitor);
            }
            foreach (DeclarationNode member in node.Members) {
                member.AcceptSyntaxTreeVisitor(_childrenVisitor);
            }
        }
    }
}
