namespace MonC.SyntaxTree
{
    public interface ITopLevelStatementVisitor
    {
        void VisitEnum(EnumNode node);
        void VisitFunctionDefinition(FunctionDefinitionNode node);
    }
}
