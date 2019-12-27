namespace MonC.VM
{
    public interface IVMDebugger
    {
        void HandleBreak();
        void HandleFinished();
        void HandleModuleAdded(VMModule module);
    }
}