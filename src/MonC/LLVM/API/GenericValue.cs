using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class GenericValue : IDisposable
    {
        private LLVMGenericValueRef _genericValue;
        public bool IsValid => _genericValue.Handle != IntPtr.Zero;

        public static implicit operator LLVMGenericValueRef(GenericValue genericValue) =>
            genericValue._genericValue;

        internal GenericValue(LLVMGenericValueRef genericValue) => _genericValue = genericValue;

        public static unsafe GenericValue FromInt(Type ty, ulong n, bool isSigned) =>
            new GenericValue(LLVMSharp.Interop.LLVM.CreateGenericValueOfInt((LLVMTypeRef) ty, n, isSigned ? 1 : 0));

        public static unsafe GenericValue FromPointer(IntPtr p) =>
            new GenericValue(LLVMSharp.Interop.LLVM.CreateGenericValueOfPointer((void*) p));

        public static unsafe GenericValue FromFloat(Type ty, double n) =>
            new GenericValue(LLVMSharp.Interop.LLVM.CreateGenericValueOfFloat((LLVMTypeRef) ty, n));

        public unsafe uint IntWidth => LLVMSharp.Interop.LLVM.GenericValueIntWidth(_genericValue);

        public unsafe ulong IntValue(bool isSigned) =>
            LLVMSharp.Interop.LLVM.GenericValueToInt(_genericValue, isSigned ? 1 : 0);

        public unsafe IntPtr PointerValue => (IntPtr) LLVMSharp.Interop.LLVM.GenericValueToPointer(_genericValue);

        public unsafe double FloatValue(Type ty) =>
            LLVMSharp.Interop.LLVM.GenericValueToFloat((LLVMTypeRef) ty, _genericValue);

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private unsafe void DoDispose()
        {
            if (_genericValue.Handle != IntPtr.Zero) {
                LLVMSharp.Interop.LLVM.DisposeGenericValue(_genericValue);
                _genericValue = new LLVMGenericValueRef();
            }
        }

        ~GenericValue() => DoDispose();
    }
}
