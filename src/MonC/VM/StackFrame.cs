
using System.Collections.Generic;

namespace MonC.VM
{
    public class StackFrame
    {
        public int Function;
        public int PC;
        public StackFrameMemory Memory = new StackFrameMemory();
        public IEnumerator<Continuation> BindingEnumerator;
    }
}