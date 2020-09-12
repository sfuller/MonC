namespace MonC.SyntaxTree
{
    public interface ITopLevelStatementVisitor
    {
        void VisitEnum(EnumLeaf leaf);
        void VisitFunctionDefinition(FunctionDefinitionLeaf leaf);
    }
}