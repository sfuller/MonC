using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MonC.LLVM
{
    internal unsafe struct MarshaledString : IDisposable
    {
        public static string NativeToManaged(sbyte* ptr)
        {
            var span = new ReadOnlySpan<byte>(ptr, int.MaxValue);
            return span.Slice(0, span.IndexOf((byte) '\0')).ToString();
        }

        public static string NativeToManagedDispose(sbyte* ptr)
        {
            string ret = NativeToManaged(ptr);
            LLVMSharp.Interop.LLVM.DisposeMessage(ptr);
            return ret;
        }

        public MarshaledString(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) {
                var value = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(value, 0, 0);

                Length = 0;
                Value = (sbyte*) value;
            } else {
                var valueBytes = Encoding.UTF8.GetBytes(input.ToString());
                var length = valueBytes.Length;
                var value = Marshal.AllocHGlobal(length + 1);
                Marshal.Copy(valueBytes, 0, value, length);
                Marshal.WriteByte(value, length, 0);

                Length = length;
                Value = (sbyte*) value;
            }
        }

        public int Length { get; private set; }

        public sbyte* Value { get; private set; }

        public void Dispose()
        {
            if (Value != null) {
                Marshal.FreeHGlobal((IntPtr) Value);
                Value = null;
                Length = 0;
            }
        }

        public static implicit operator sbyte*(in MarshaledString value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            var span = new ReadOnlySpan<byte>(Value, Length);
            return span.ToString();
        }
    }
}
