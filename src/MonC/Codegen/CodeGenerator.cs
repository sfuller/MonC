using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.Parsing;
using MonC.Semantics;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenerator
    {
        private readonly ParseModule _module;
        private readonly SemanticContext _semanticContext;
        private readonly FunctionManager _manager = new FunctionManager();

        public CodeGenerator(ParseModule module, SemanticContext semanticContext)
        {
            _module = module;
            _semanticContext = semanticContext;
        }

        public ILModule Generate()
        {
            foreach (FunctionDefinitionNode function in _module.Functions) {
                _manager.RegisterFunction(function);
            }

            List<ILFunction> functions = new List<ILFunction>();
            List<string> strings = new List<string>();

            foreach (FunctionDefinitionNode function in _module.Functions) {
                functions.Add(GenerateFunction(function, strings));
            }

            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _manager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _manager.UndefinedFunctions.Keys.ToArray(),
                Strings = strings.ToArray()
            };

        }

        private ILFunction GenerateFunction(FunctionDefinitionNode node, List<string> strings)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            layoutGenerator.VisitFunctionDefinition(node);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            FunctionBuilder builder = new FunctionBuilder(layout, _module.SymbolMap);
            FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(builder, layout, _manager, strings, _semanticContext.EnumInfo);
            functionCodeGenVisitor.VisitBody(node.Body);

            if (builder.InstructionCount == 0 || builder.Instructions[builder.InstructionCount - 1].Op != OpCode.RETURN) {
                builder.AddInstruction(OpCode.RETURN);
            }

            return builder.Build(node);
        }

    }
}
