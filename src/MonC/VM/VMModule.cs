using System.Collections.Generic;
using MonC.Codegen;

namespace MonC.VM
{
    public class VMModule
    {
        public ILModule Module;
        public Dictionary<int, VMEnumerable> VMFunctions;
    }
}