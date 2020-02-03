using System;
using JetBrains.Annotations;

namespace MonC.VM
{
    [MeansImplicitUse(ImplicitUseTargetFlags.Members)]
    [AttributeUsage(AttributeTargets.Enum)]
    public class LinkableEnumAttribute : Attribute
    {
        public string? Prefix;
    }
}