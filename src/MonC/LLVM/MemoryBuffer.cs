using System;

namespace MonC.LLVM
{
    public class MemoryBuffer : IDisposable
    {
        private CAPI.LLVMMemoryBufferRef _memoryBuffer;

        public static implicit operator CAPI.LLVMMemoryBufferRef(MemoryBuffer memoryBuffer) =>
            memoryBuffer._memoryBuffer;

        internal MemoryBuffer(CAPI.LLVMMemoryBufferRef memoryBuffer) => _memoryBuffer = memoryBuffer;

        public static MemoryBuffer WithContentsOfFile(string path)
        {
            if (!CAPI.LLVMCreateMemoryBufferWithContentsOfFile(path, out CAPI.LLVMMemoryBufferRef buffer,
                out string? message)) {
                throw new InvalidOperationException(message != null
                    ? message
                    : $"Cannot create memory buffer from {path}");
            }

            return new MemoryBuffer(buffer);
        }

        public static MemoryBuffer WithBytes(byte[] bytes, string bufferName) =>
            new MemoryBuffer(CAPI.LLVMCreateMemoryBufferWithMemoryRangeCopy(bytes, bufferName));

        public int Length => (int) CAPI.LLVMGetBufferSize(_memoryBuffer);

        public byte[] Bytes => CAPI.LLVMGetBufferStartBytes(_memoryBuffer);

        public string GetAsString() => CAPI.LLVMGetBufferString(_memoryBuffer);

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_memoryBuffer.IsValid) {
                CAPI.LLVMDisposeMemoryBuffer(_memoryBuffer);
                _memoryBuffer = new CAPI.LLVMMemoryBufferRef();
            }
        }

        ~MemoryBuffer() => DoDispose();
    }
}
