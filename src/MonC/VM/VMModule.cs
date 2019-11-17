using System.Collections.Generic;
using MonC.Codegen;

namespace MonC.VM
{
    public class VMModule
    {
        public readonly ILModule Module;
        public readonly Dictionary<int, VMFunction> VMFunctions;

        public VMModule()
        {
            Module = new ILModule();
            VMFunctions = new Dictionary<int, VMFunction>();
        }
        
        public VMModule(ILModule module, Dictionary<int, VMFunction> vmFunctions)
        {
            Module = module;
            VMFunctions = vmFunctions;
        }
    }
}