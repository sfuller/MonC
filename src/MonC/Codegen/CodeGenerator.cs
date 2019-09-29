using System.Collections.Generic;
using System.Linq;
using MonC.Bytecode;
using MonC.Parsing;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class CodeGenerator
    {
        private FunctionManager _manager;
        
        public ILModule Generate(ParseModule module)
        {
            _manager = new FunctionManager();

            foreach (FunctionDefinitionLeaf function in module.Functions) {
                _manager.RegisterFunction(function);
            }

            List<ILFunction> functions = new List<ILFunction>();
            List<string> strings = new List<string>();
            
            foreach (FunctionDefinitionLeaf function in module.Functions) {
                functions.Add(GenerateFunction(module, function, strings));
            }
            
            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _manager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _manager.UndefinedFunctions.Keys.ToArray(),
                Strings = strings.ToArray()
            };

        }

        private ILFunction GenerateFunction(ParseModule module, FunctionDefinitionLeaf leaf, List<string> strings)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            leaf.Accept(layoutGenerator);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            
            CodeGenVisitor codeGenVisitor = new CodeGenVisitor(layout, _manager, module.TokenMap);
            leaf.Accept(codeGenVisitor);

            strings.AddRange(codeGenVisitor.GetStrings());
            
            return codeGenVisitor.MakeFunction();
        }
        
    }
}