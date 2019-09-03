using System.Collections.Generic;

namespace MonC.VM
{
    public class VMEnumerableWrapper
    {
        private readonly VMFunction _func;

        public VMEnumerableWrapper(VMFunction func)
        {
            _func = func;
        }
        
        public IEnumerator<Continuation> MakeEnumerator(IVMBindingContext context, int[] args)
        {
            yield return Continuation.Return(_func(args));
        }
        
    }
}