using System.Collections.Generic;
using System.Linq;
using MonC.IL;
using MonC.Parsing;
using MonC.SyntaxTree;

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

            foreach (FunctionDefinitionNode function in module.Functions) {
                functions.Add(GenerateFunction(module, function, strings));
            }

            ProcessEnums(module, enumerations);

            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _manager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _manager.UndefinedFunctions.Keys.ToArray(),
                ExportedEnumValues = enumerations.ToArray(),
                Strings = strings.ToArray()
            };

        }

        private ILFunction GenerateFunction(ParseModule module, FunctionDefinitionNode node, List<string> strings)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            layoutGenerator.VisitFunctionDefinition(node);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            FunctionBuilder builder = new FunctionBuilder(layout, module.SymbolMap);
            FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(builder, layout, _manager, strings);
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
                    KeyValuePair<string, int>[] enumerations = enumNode.Enumerations;
                    for (int i = 0, ilen = enumerations.Length; i < ilen; ++i) {
                        var enumeration = enumerations[i];
                        exportedEnums[enumeration.Key] = enumeration.Value;
                    }
                }
            }
        }

    }
}
