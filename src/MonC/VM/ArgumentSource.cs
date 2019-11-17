namespace MonC.VM
{
    public readonly struct ArgumentSource
    {
        private readonly StackFrameMemory _backing;
        private readonly int _argumentStackStart;

        public ArgumentSource(StackFrameMemory backing, int argumentStackStart)
        {
            _backing = backing;
            _argumentStackStart = argumentStackStart;
        }
        
        public int GetArgument(int index)
        {
            return _backing.Read(_argumentStackStart + index);
        }
    }
}