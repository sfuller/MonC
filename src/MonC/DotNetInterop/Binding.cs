using MonC.SyntaxTree;
using MonC.VM;

namespace MonC.DotNetInterop
{
    public struct Binding
    {
        public FunctionDefinitionNode Prototype;
        public VMFunction Implementation;
    }
}