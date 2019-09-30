using System;
using JetBrains.Annotations;

namespace MonC.VM
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class LinkableFunctionAttribute : Attribute
    {
        public string Name;
        public int ArgumentCount;
    }
}