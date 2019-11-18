namespace MonC.VM
{
    public interface IVMDebugger
    {
        void HandleBreak();
        void HandleFinished();
    }
}