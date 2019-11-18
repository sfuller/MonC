namespace MonC.VM
{
    public interface IDebuggableVM
    {
        bool IsPaused { get; }
        void Continue();
        void SetStepping(bool isStepping);
    }
}