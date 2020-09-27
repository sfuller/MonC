namespace MonC.SyntaxTree
{
    public interface ITopLevelStatementVisitor
    {
        void VisitEnum(EnumNode node);
        void VisitFunctionDefinition(FunctionDefinitionNode node);
        void VisitStruct(StructNode node);
    }
}
