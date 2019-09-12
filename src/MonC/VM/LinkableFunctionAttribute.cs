using System;

namespace MonC.VM
{
    public class LinkableFunctionAttribute : Attribute
    {
        public string Name;
        public int ArgumentCount;
    }
}