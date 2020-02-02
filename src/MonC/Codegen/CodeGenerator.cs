using System.Collections.Generic;
using System.Linq;
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
            leaf.Accept(layoutGenerator);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            
            CodeGenVisitor codeGenVisitor = new CodeGenVisitor(layout, _manager, module.TokenMap, strings);
            leaf.Accept(codeGenVisitor);

            return codeGenVisitor.MakeFunction();
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