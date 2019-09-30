using System;
using JetBrains.Annotations;

namespace MonC.VM
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class LinkableModuleAttribute : Attribute
    {
    }
}