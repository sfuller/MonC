namespace MonC.VM
{
    public interface IVMBindingContext
    {
        int ReturnValue { get; }

        string GetString(int id);
    }
}