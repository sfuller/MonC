namespace MonC.VM
{
    //public delegate int VMFunctionDelegate(ArgumentSource arguments);
    public delegate void VMFunctionDelegate(IVMBindingContext context, ArgumentSource arguments);

    public struct VMFunction
    {
        /// <summary>
        /// How much memory needs to be used from the caller's argument stack.
        /// </summary>
        public int ArgumentMemorySize;

        public VMFunctionDelegate Delegate;
    }
}
