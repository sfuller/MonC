using System;

namespace MonC.LLVM
{
    public class TargetData : IDisposable
    {
        private CAPI.LLVMTargetDataRef _targetData;

        public static implicit operator CAPI.LLVMTargetDataRef(TargetData targetData) => targetData._targetData;

        internal TargetData(CAPI.LLVMTargetDataRef targetData) => _targetData = targetData;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (_targetData.IsValid) {
                CAPI.LLVMDisposeTargetData(_targetData);
                _targetData = new CAPI.LLVMTargetDataRef();
            }
        }

        ~TargetData() => DoDispose();

        public ulong SizeOfTypeInBits(Type ty) => CAPI.LLVMSizeOfTypeInBits(_targetData, ty);

        public ulong StoreSizeOfType(Type ty) => CAPI.LLVMStoreSizeOfType(_targetData, ty);

        public ulong ABISizeOfType(Type ty) => CAPI.LLVMABISizeOfType(_targetData, ty);

        public uint ABIAlignmentOfType(Type ty) => CAPI.LLVMABIAlignmentOfType(_targetData, ty);

        public uint CallFrameAlignmentOfType(Type ty) => CAPI.LLVMCallFrameAlignmentOfType(_targetData, ty);

        public uint PreferredAlignmentOfType(Type ty) => CAPI.LLVMPreferredAlignmentOfType(_targetData, ty);
    }
}
