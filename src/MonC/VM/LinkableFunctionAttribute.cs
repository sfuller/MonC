using System;

namespace MonC.VM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LinkableFunctionAttribute : Attribute
    {
        public readonly string Name;

        public LinkableFunctionAttribute(string name = null)
        {
            Name = name;
        }
    }
}