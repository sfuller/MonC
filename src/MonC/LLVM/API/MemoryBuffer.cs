using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class MemoryBuffer : IDisposable
    {
        private LLVMMemoryBufferRef _memoryBuffer;

        internal void Release() => _memoryBuffer = new LLVMMemoryBufferRef();

        public static implicit operator LLVMMemoryBufferRef(MemoryBuffer memoryBuffer) =>
            memoryBuffer._memoryBuffer;

        internal MemoryBuffer(LLVMMemoryBufferRef memoryBuffer) => _memoryBuffer = memoryBuffer;

        public static unsafe MemoryBuffer WithContentsOfFile(string path)
        {
            using var marshaledPath = new MarshaledString(path.AsSpan());
            LLVMMemoryBufferRef buffer;
            sbyte* message;
            if (LLVMSharp.Interop.LLVM.CreateMemoryBufferWithContentsOfFile(marshaledPath,
                (LLVMOpaqueMemoryBuffer**) &buffer, &message) != 0) {
                throw new InvalidOperationException(message != null
                    ? MarshaledString.NativeToManagedDispose(message)
                    : $"Cannot create memory buffer from {path}");
            }

            return new MemoryBuffer(buffer);
        }

        public static unsafe MemoryBuffer WithBytes(byte[] bytes, string bufferName)
        {
            using var marshaledName = new MarshaledString(bufferName.AsSpan());
            fixed (byte* dataPtr = bytes.AsSpan()) {
                return new MemoryBuffer(LLVMSharp.Interop.LLVM.CreateMemoryBufferWithMemoryRangeCopy((sbyte*) dataPtr,
                    (UIntPtr) bytes.Length, marshaledName));
            }
        }

        public unsafe int Length => (int) LLVMSharp.Interop.LLVM.GetBufferSize(_memoryBuffer);

        public unsafe ReadOnlySpan<byte> Bytes => new ReadOnlySpan<byte>(
            LLVMSharp.Interop.LLVM.GetBufferStart(_memoryBuffer), Length);

        public string GetAsString() => Bytes.ToString();

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private unsafe void DoDispose()
        {
            if (_memoryBuffer.Handle != IntPtr.Zero) {
                LLVMSharp.Interop.LLVM.DisposeMemoryBuffer(_memoryBuffer);
                _memoryBuffer = new LLVMMemoryBufferRef();
            }
        }

        ~MemoryBuffer() => DoDispose();
    }
}
