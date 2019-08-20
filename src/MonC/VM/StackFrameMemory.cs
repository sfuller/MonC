using System.Collections.Generic;

namespace MonC.VM
{
    public class StackFrameMemory
    {
        private List<int> _data = new List<int>();

        public int Read(int address)
        {
            if (address < 0 || address >= _data.Count) {
                return 0;
            }
            return _data[address];
        }

        public void Write(int address, int value)
        {
            while (address >= _data.Count) {
                _data.Add(0);
            }
            _data[address] = value;
        }
    }
}