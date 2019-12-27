using System.Collections.Generic;

namespace MonC.VM
{
    public class StackFrame
    {
        public VMModule Module = VMModule.Default;
        public int Function;
        public int PC;
        public readonly StackFrameMemory Memory = new StackFrameMemory();
        public IEnumerator<Continuation>? BindingEnumerator;
    }
}