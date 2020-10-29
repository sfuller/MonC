using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;

namespace MonC.Semantics.Pointers
{
    public class BorrowUsageAnalyzer : IVisitor<ReturnNode>
    {
        public void Process(FunctionDefinitionNode function)
        {
            SyntaxTreeDelegator syntaxTreeDelegator = new SyntaxTreeDelegator();
            StatementDelegator statementDelegator = new StatementDelegator();
            syntaxTreeDelegator.StatementVisitor = statementDelegator;
            statementDelegator.ReturnVisitor = this;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor =
                new StatementChildrenVisitor(syntaxTreeDelegator, childrenVisitor);
            childrenVisitor.StatementVisitor = statementChildrenVisitor;

            function.Body.VisitStatements(statementChildrenVisitor);
        }

        public void Visit(ReturnNode node)
        {

        }
    }
}
