using System.Collections.Generic;

namespace MonC.VM
{
    public delegate int VMFunction(int[] arguments);
    public delegate IEnumerator<Continuation> VMEnumerable(IVMBindingContext context, int[] arguments);
}
