using System;

namespace MonC.VM
{
    public class StackFrameMemory
    {
        private byte[] _data = new byte[0];
        private int _lengthInuse;
        private int _stackPointer;

        public int Size => _lengthInuse;
        public int StackPointer => _stackPointer;

        /// <summary>
        /// Prepare the backing memory to be used. Previous contents may be lost after, not to be used for in-use stack
        /// memory.
        /// </summary>
        public void Recreate(int newSize)
        {
            _lengthInuse = newSize;
            _stackPointer = 0;
            if (_data.Length < newSize) {
                _data = new byte[newSize];
            }
        }

        public byte Read(int address)
        {
            return _data[address];
        }

        public void Write(int address, byte value)
        {
            _data[address] = value;
        }

        public void CopyFrom(StackFrameMemory source, int sourceOffset, int destOffset, int size)
        {
            Array.Copy(source._data, sourceOffset, _data, destOffset, size);
        }

        public void CopyTo(int sourceOffset, int destinationOffset, int size, byte[] destination)
        {
            Array.Copy(_data, sourceOffset, destination, destinationOffset, size);
        }

        public void PushFrom(StackFrameMemory source, int sourcePointer, int size)
        {
            Array.Copy(source._data, sourcePointer, _data, _stackPointer, size);
            _stackPointer += size;
        }

        public void PushVal(byte value)
        {
            Write(_stackPointer++, value);
        }

        public byte PopVal()
        {
            return Read(--_stackPointer);
        }

        public void PushValInt(int val)
        {
            int stackStart = _stackPointer;
            unchecked {
                Write(stackStart, (byte) (val >> 24));
                Write(stackStart + 1, (byte) (val >> 16));
                Write(stackStart + 2, (byte) (val >> 8));
                Write(stackStart + 3, (byte) val);
            }
            _stackPointer += 4;
        }

        public int PopValInt()
        {
            int stackPointer = _stackPointer;
            byte byte1 = Read(stackPointer - 1);
            byte byte2 = Read(stackPointer - 2);
            byte byte3 = Read(stackPointer - 3);
            byte byte4 = Read(stackPointer - 4);
            _stackPointer -= 4;
            return (byte4 << 24) + (byte3 << 16) + (byte2 << 8) + byte1;
        }

        public void Push(int size)
        {
            _stackPointer += size;
        }

        public void Discard(int size)
        {
            _stackPointer -= size;
        }

        public void Access(int offset, int size)
        {
            int destPointer = _stackPointer - size;
            Array.Copy(_data, _stackPointer, _data, destPointer, size - offset);
            _stackPointer -= size - offset;
        }
    }
}
