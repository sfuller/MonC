using System;
using MonC.VM;

namespace CoreLib
{
    [LinkableModule]
    public static class IO
    {
        [LinkableFunction(Name = "print", ArgumentCount = 1)]
        public static void Print(IVMBindingContext context, ArgumentSource args)
        {
            Console.WriteLine("monctest:" + context.GetString(args.GetArgument(0)));
        }
    }
}
