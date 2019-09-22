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

            foreach (FunctionDefinitionLeaf function in module.Functions) {
                functions.Add(GenerateFunction(module, function));
            }
            
            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                ExportedFunctions = _manager.ExportedFunctions.ToArray(),
                UndefinedFunctionNames = _manager.UndefinedFunctions.Keys.ToArray()
            };

        }

        private ILFunction GenerateFunction(ParseModule module, FunctionDefinitionLeaf leaf)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            leaf.Accept(layoutGenerator);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            
            List<Instruction> instructions = new List<Instruction>();
            Dictionary<int, Symbol> symbols = new Dictionary<int, Symbol>();

            CodeGenVisitor codeGenVisitor = new CodeGenVisitor(layout, instructions, _manager, symbols, module.TokenMap);
            leaf.Accept(codeGenVisitor);

            return new ILFunction {
                Code = instructions.ToArray(),
                Symbols = symbols
            };
        }
        
    }
}