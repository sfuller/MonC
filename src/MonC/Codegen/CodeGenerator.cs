using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.Semantics;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenerator
    {
        private readonly SemanticModule _module;
        private readonly SemanticContext _semanticContext;
        private readonly FunctionManager _functionManager = new FunctionManager();
        private readonly StructLayoutManager _structLayoutManager = new StructLayoutManager();
        private readonly TypeSizeManager _typeSizeManager;

        public CodeGenerator(SemanticModule module, SemanticContext semanticContext)
        {
            _module = module;
            _semanticContext = semanticContext;

            _typeSizeManager = new TypeSizeManager(_structLayoutManager);
            _structLayoutManager.Setup(new StructLayoutGenerator(_typeSizeManager));
        }

        public ILModule Generate()
        {
            foreach (FunctionDefinitionNode function in _module.BaseModule.Functions) {
                _functionManager.RegisterFunction(function);
            }

            List<ILFunction> functions = new List<ILFunction>();
            List<string> strings = new List<string>();

            foreach (FunctionDefinitionNode function in _module.BaseModule.Functions) {
                functions.Add(GenerateFunction(function, strings));
            }

            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _functionManager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _functionManager.UndefinedFunctions.Keys.ToArray(),
                Strings = strings.ToArray()
            };

        }

        private ILFunction GenerateFunction(FunctionDefinitionNode node, List<string> strings)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            layoutGenerator.VisitFunctionDefinition(node);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            FunctionBuilder builder = new FunctionBuilder(layout, _module.BaseModule.SymbolMap);
            FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(
                    builder, layout, _functionManager, _module, _semanticContext, _structLayoutManager,
                    _typeSizeManager, strings);

            builder.AddInstruction(OpCode.PUSH, 0, layout.EndAddress);
            functionCodeGenVisitor.VisitBody(node.Body);

            if (builder.InstructionCount == 0 || builder.Instructions[builder.InstructionCount - 1].Op != OpCode.RETURN) {
                builder.AddInstruction(OpCode.RETURN);
            }

            return builder.Build(node);
        }

    }
}
