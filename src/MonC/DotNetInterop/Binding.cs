using MonC.SyntaxTree;
using MonC.VM;

namespace MonC.DotNetInterop
{
    public struct Binding
    {
        public FunctionDefinitionLeaf Prototype;
        public VMEnumerable Implementation;
    }
}