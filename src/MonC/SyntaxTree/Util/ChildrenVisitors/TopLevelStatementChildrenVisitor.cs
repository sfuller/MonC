using MonC.SyntaxTree.Nodes;

namespace MonC.SyntaxTree.Util.ChildrenVisitors
{
    public class TopLevelStatementChildrenVisitor : ITopLevelStatementVisitor
    {
        private readonly ISyntaxTreeVisitor _visitor;

        public TopLevelStatementChildrenVisitor(ISyntaxTreeVisitor visitor)
        {
            _visitor = visitor;
        }

        public void VisitEnum(EnumNode node)
        {
            _visitor.VisitTopLevelStatement(node);
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            _visitor.VisitTopLevelStatement(node);
            _visitor.VisitSpecifier(node.ReturnType);
            for (int i = 0, ilen = node.Parameters.Length; i < ilen; ++i) {
                _visitor.VisitStatement(node.Parameters[i]);
            }
            _visitor.VisitStatement(node.Body);
        }
    }
}
