using System;
using JetBrains.Annotations;

namespace MonC.VM
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Enum)]
    public class LinkableEnumAttribute : Attribute
    {
        public string Prefix;
    }
}