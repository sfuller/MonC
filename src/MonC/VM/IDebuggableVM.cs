namespace MonC.VM
{
    public interface IDebuggableVM
    {
        void Continue();
        void SetStepping(bool isStepping);
    }
}