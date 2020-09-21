using MonC.SyntaxTree;

namespace MonC.Parsing
{
    /// <summary>
    /// A visitor that places the visited top level statements into a <see cref="ParseModule"/>
    /// </summary>
    public class ParseModuleHopper : ITopLevelStatementVisitor
    {
        private readonly ParseModule _module;

        public ParseModuleHopper(ParseModule module)
        {
            _module = module;
        }

        public void VisitEnum(EnumNode node)
        {
            _module.Enums.Add(node);
        }

        public void VisitFunctionDefinition(FunctionDefinitionNode node)
        {
            _module.Functions.Add(node);
        }

        public void VisitStruct(StructNode node)
        {
            _module.Structs.Add(node);
        }
    }
}
