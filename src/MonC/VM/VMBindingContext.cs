using System.Collections.Generic;

namespace MonC.VM
{
    public class VMBindingContext : IVMBindingContext
    {
        public IEnumerator<Continuation> Enumerator;
        
        public int ReturnValue { get; set; }
    }
}