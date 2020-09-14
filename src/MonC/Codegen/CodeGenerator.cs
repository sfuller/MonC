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
            foreach (FunctionDefinitionLeaf function in module.Functions) {
                _manager.RegisterFunction(function);
            }

            List<ILFunction> functions = new List<ILFunction>();
            List<string> strings = new List<string>();
            Dictionary<string, int> enumerations = new Dictionary<string, int>();

            foreach (FunctionDefinitionLeaf function in module.Functions) {
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

        private ILFunction GenerateFunction(ParseModule module, FunctionDefinitionLeaf leaf, List<string> strings)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            layoutGenerator.VisitFunctionDefinition(leaf);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            FunctionBuilder builder = new FunctionBuilder(layout, module.TokenMap);
            FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(builder, layout, _manager, strings);
            leaf.Body.AcceptStatements(functionCodeGenVisitor);

            if (builder.InstructionCount == 0 || builder.Instructions[builder.InstructionCount - 1].Op != OpCode.RETURN) {
                builder.AddInstruction(OpCode.RETURN);
            }

            return builder.Build(leaf);
        }

        private static void ProcessEnums(ParseModule module, IDictionary<string, int> exportedEnums)
        {
            foreach (EnumLeaf enumLeaf in module.Enums) {
                if (enumLeaf.IsExported) {
                    KeyValuePair<string, int>[] enumerations = enumLeaf.Enumerations;
                    for (int i = 0, ilen = enumerations.Length; i < ilen; ++i) {
                        var enumeration = enumerations[i];
                        exportedEnums[enumeration.Key] = enumeration.Value;
                    }
                }
            }
        }

    }
}
