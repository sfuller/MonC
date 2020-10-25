using System;

namespace MonC.LLVM
{
    public class GenericValue : IDisposable
    {
        private CAPI.LLVMGenericValueRef _genericValue;
        public bool IsValid => _genericValue.IsValid;

        public static implicit operator CAPI.LLVMGenericValueRef(GenericValue genericValue) =>
            genericValue._genericValue;

        internal GenericValue(CAPI.LLVMGenericValueRef genericValue) => _genericValue = genericValue;

        public static GenericValue FromInt(Type ty, ulong n, bool isSigned) =>
            new GenericValue(CAPI.LLVMCreateGenericValueOfInt(ty, n, isSigned));

        public static GenericValue FromPointer(IntPtr p) => new GenericValue(CAPI.LLVMCreateGenericValueOfPointer(p));

        public static GenericValue FromFloat(Type ty, double n) =>
            new GenericValue(CAPI.LLVMCreateGenericValueOfFloat(ty, n));

        public uint IntWidth => CAPI.LLVMGenericValueIntWidth(_genericValue);

        public ulong IntValue(bool isSigned) => CAPI.LLVMGenericValueToInt(_genericValue, isSigned);

        public IntPtr PointerValue => CAPI.LLVMGenericValueToPointer(_genericValue);

        public double FloatValue(Type ty) => CAPI.LLVMGenericValueToFloat(ty, _genericValue);

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_genericValue.IsValid) {
                CAPI.LLVMDisposeGenericValue(_genericValue);
                _genericValue = new CAPI.LLVMGenericValueRef();
            }
        }

        ~GenericValue() => DoDispose();
    }
}
