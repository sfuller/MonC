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

        
        public ILModule Generate(Module module)
        {
            _manager = new FunctionManager();

            foreach (FunctionDefinitionLeaf function in module.Functions) {
                _manager.RegisterFunction(function);
            }

            List<Instruction[]> functions = new List<Instruction[]>();
            
            foreach (FunctionDefinitionLeaf function in module.Functions) {
                functions.Add(GenerateFunction(function));
            }
            
            return new ILModule {
                DefinedFunctions = functions.ToArray(),
                DefinedFunctionsIndices = _manager.DefinedFunctions,
                ExportedFunctions = _manager.DefinedFunctions.Keys.ToArray(),
                UndefinedFunctionsIndices = _manager.UndefinedFunctions
            };

        }

        private Instruction[] GenerateFunction(FunctionDefinitionLeaf leaf)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            leaf.Accept(layoutGenerator);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            
            List<Instruction> instructions = new List<Instruction>();
            
            CodeGenVisitor codeGenVisitor = new CodeGenVisitor(layout, instructions, _manager);
            leaf.Accept(codeGenVisitor);

            return instructions.ToArray();
        }
        
    }
}