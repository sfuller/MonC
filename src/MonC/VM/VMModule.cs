using System.Collections.Generic;
using MonC.IL;

namespace MonC.VM
{
    public class VMModule
    {
        public readonly ILModule ILModule;
        public readonly Dictionary<int, VMFunction> VMFunctions;

        public VMModule()
        {
            ILModule = new ILModule();
            VMFunctions = new Dictionary<int, VMFunction>();
        }

        public VMModule(ILModule ilModule, Dictionary<int, VMFunction> vmFunctions)
        {
            ILModule = ilModule;
            VMFunctions = vmFunctions;
        }

        public static readonly VMModule Default = new VMModule();
    }
}
