using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.Parsing;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.Codegen
{
    public class CodeGenerator
    {
        private readonly FunctionManager _manager = new FunctionManager();

        public ILModule Generate(ParseModule module)
        {
            foreach (FunctionDefinitionNode function in module.Functions) {
                _manager.RegisterFunction(function);
            }

            List<ILFunction> functions = new List<ILFunction>();
            List<string> strings = new List<string>();
            Dictionary<string, int> enumerations = new Dictionary<string, int>();

            ProcessEnums(module, enumerations);

            foreach (FunctionDefinitionNode function in module.Functions) {
                functions.Add(GenerateFunction(module, function, strings, enumerations));
            }

            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _manager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _manager.UndefinedFunctions.Keys.ToArray(),
                ExportedEnumValues = enumerations.ToArray(),
                Strings = strings.ToArray()
            };

        }

        private ILFunction GenerateFunction(ParseModule module, FunctionDefinitionNode node, List<string> strings, Dictionary<string, int> enums)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            layoutGenerator.VisitFunctionDefinition(node);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            FunctionBuilder builder = new FunctionBuilder(layout, module.SymbolMap);
            FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(builder, layout, _manager, strings, enums);
            functionCodeGenVisitor.VisitBody(node.Body);

            if (builder.InstructionCount == 0 || builder.Instructions[builder.InstructionCount - 1].Op != OpCode.RETURN) {
                builder.AddInstruction(OpCode.RETURN);
            }

            return builder.Build(node);
        }

        private static void ProcessEnums(ParseModule module, IDictionary<string, int> exportedEnums)
        {
            foreach (EnumNode enumNode in module.Enums) {
                if (enumNode.IsExported) {
                    List<EnumDeclarationNode> enumDeclarations = enumNode.Declarations;
                    for (int i = 0, ilen = enumDeclarations.Count; i < ilen; ++i) {
                        EnumDeclarationNode declaration = enumDeclarations[i];
                        exportedEnums[declaration.Name] = i;
                    }
                }
            }
        }

    }
}
