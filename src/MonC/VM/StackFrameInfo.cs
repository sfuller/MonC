namespace MonC.VM
{
    public struct StackFrameInfo
    {
        public VMModule Module;
        public int Function;
        public int PC;
    }
}