using System;

namespace MonC.VM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ExternalFunctionAttribute : Attribute
    {
        public readonly string Name;

        public ExternalFunctionAttribute(string? name = null)
        {
            if (name == null) {
                name = string.Empty;
            }
            Name = name;
        }
    }
}