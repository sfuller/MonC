namespace MonC.SyntaxTree.Util.ReplacementVisitors
{
    public class ProcessTopLevelStatementReplacementsVisitor : ITopLevelStatementVisitor
    {
        public readonly ReplacementProcessor _processor;

        public ProcessTopLevelStatementReplacementsVisitor(IReplacementSource replacementSource)
        {
            _processor = new ReplacementProcessor(replacementSource);
        }

        public void VisitEnum(EnumNode node) { }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            node.ReturnType = _processor.ProcessReplacement(node.ReturnType);
            for (int i = 0, ilen = node.Parameters.Length; i < ilen; ++i) {
                node.Parameters[i] = _processor.ProcessReplacement(node.Parameters[i]);
            }
        }
    }
}
