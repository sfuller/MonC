using System.Collections.Generic;
using MonC.Codegen;

namespace MonC.VM
{
    public class VMModule
    {
        public readonly ILModule Module;
        public readonly Dictionary<int, VMEnumerable> VMFunctions;

        public VMModule()
        {
            Module = new ILModule();
            VMFunctions = new Dictionary<int, VMEnumerable>();
        }
        
        public VMModule(ILModule module, Dictionary<int, VMEnumerable> vmFunctions)
        {
            Module = module;
            VMFunctions = vmFunctions;
        }
    }
}