using System;
using System.Runtime.InteropServices;

namespace MonC.VM
{
    public sealed class StackFrameMemory : IDisposable
    {
        private IntPtr _data;
        private int _capacity;
        private int _stackPointer;

        public int Size => _capacity;
        public int StackPointer => _stackPointer;

        private void ReleaseUnmanagedResources()
        {
            Marshal.FreeHGlobal(_data);
            _data = IntPtr.Zero;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            _capacity = 0;
            GC.SuppressFinalize(this);
        }

        ~StackFrameMemory() {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        /// Prepare the backing memory to be used. Previous contents may be lost after, not to be used for in-use stack
        /// memory.
        /// </summary>
        public void Recreate(int newSize)
        {
            ReleaseUnmanagedResources();
            _data = Marshal.AllocHGlobal(newSize);
            _capacity = newSize;
            _stackPointer = 0;
        }

        public byte Read(int address)
        {
            if (address < 0 || address >= _capacity) {
                throw new IndexOutOfRangeException();
            }
            unsafe {
                return ((byte*)_data.ToPointer())[address];
            }
        }

        public void Write(int address, byte value)
        {
            if (address < 0 || address >= _capacity) {
                throw new IndexOutOfRangeException();
            }
            unsafe {
                ((byte*)_data.ToPointer())[address] = value;
            }
        }

        public void CopyFrom(StackFrameMemory source, int sourceOffset, int destOffset, int size)
        {
            unsafe {
                if (sourceOffset + size > source._capacity || sourceOffset < 0 || destOffset < 0) {
                    throw new IndexOutOfRangeException();
                }
                void* sourcePtr = IntPtr.Add(source._data, sourceOffset).ToPointer();
                void* destPtr = IntPtr.Add(_data, destOffset).ToPointer();
                Buffer.MemoryCopy(sourcePtr, destPtr, _capacity - destOffset, size);
            }
        }

        public void CopyTo(int sourceOffset, int destinationOffset, int size, byte[] destination)
        {
            unsafe {
                if (sourceOffset + size > _capacity || sourceOffset < 0 || destinationOffset < 0) {
                    throw new IndexOutOfRangeException();
                }
                fixed (byte* destPtrBase = destination) {
                    void* sourcePtr = IntPtr.Add(_data, sourceOffset).ToPointer();
                    void* destPtr = &destPtrBase[destinationOffset];
                    Buffer.MemoryCopy(sourcePtr, destPtr, destination.Length - destinationOffset, size);
                }
            }
        }

        public void PushFrom(StackFrameMemory source, int sourcePointer, int size)
        {
            unsafe {
                if (sourcePointer + size > source._capacity || sourcePointer < 0 || _stackPointer < 0) {
                    throw new IndexOutOfRangeException();
                }
                void* sourcePtr = IntPtr.Add(source._data, sourcePointer).ToPointer();
                void* destPtr = IntPtr.Add(_data, _stackPointer).ToPointer();
                Buffer.MemoryCopy(sourcePtr, destPtr, _capacity - _stackPointer, size);
            }
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
            unsafe {
                if (_stackPointer - size + offset > _capacity || _stackPointer < 0 || _stackPointer - size < 0) {
                    throw new IndexOutOfRangeException();
                }
                void* sourcePtr = IntPtr.Add(_data, _stackPointer - size + offset).ToPointer();
                void* destPtr = IntPtr.Add(_data, _stackPointer - size).ToPointer();
                Buffer.MemoryCopy(sourcePtr, destPtr, _capacity - _stackPointer + size, size - offset);
            }
            _stackPointer -= offset;
        }
    }
}
