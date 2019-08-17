using System.Collections.Generic;
using MonC.Bytecode;
using MonC.SyntaxTree;

namespace MonC.Codegen
{
    public class FunctionCodeGenerator
    {



        public List<Instruction> FunctionGenerator(FunctionDefinitionLeaf leaf)
        {
            StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
            leaf.Accept(layoutGenerator);
            FunctionStackLayout layout = layoutGenerator.GetLayout();
            
            List<Instruction> instructions = new List<Instruction>();
            
            CodeGenVisitor codeGenVisitor = new CodeGenVisitor(layout, instructions);
            leaf.Accept(codeGenVisitor);

            return instructions;
        }





    }
}