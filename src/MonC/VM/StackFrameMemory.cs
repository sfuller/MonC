using System;

namespace MonC.VM
{
    public class StackFrameMemory
    {
        private int[] _data = new int[0];
        private int _lengthInuse;

        public int Size => _lengthInuse;
        
        /// <summary>
        /// Prepare the backing memory to be used. Previous contents may be lost after, not to be used for in-use stack
        /// memory.
        /// </summary>
        public void Recreate(int newSize)
        {
            _lengthInuse = newSize;
            if (_data.Length < newSize) {
                _data = new int[newSize];
            }
        }
        
        public int Read(int address)
        {
            return _data[address];
        }

        public void Write(int address, int value)
        {
            _data[address] = value;
        }

        public void CopyFrom(StackFrameMemory source, int sourceOffset, int destOffset, int size)
        {
            Array.Copy(source._data, sourceOffset, _data, destOffset, size);
        }
    }
}