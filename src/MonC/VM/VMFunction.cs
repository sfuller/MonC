using System;

namespace MonC.VM
{
    public delegate void VMFunction(int[] arguments, Action<int> ret);
}
