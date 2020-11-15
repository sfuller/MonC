using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public class TargetData : IDisposable
    {
        private LLVMTargetDataRef _targetData;

        public static implicit operator LLVMTargetDataRef(TargetData targetData) => targetData._targetData;

        internal TargetData(LLVMTargetDataRef targetData) => _targetData = targetData;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private unsafe void DoDispose()
        {
            if (_targetData.Handle != IntPtr.Zero) {
                LLVMSharp.Interop.LLVM.DisposeTargetData(_targetData);
                _targetData = new LLVMTargetDataRef();
            }
        }

        ~TargetData() => DoDispose();

        public unsafe ulong SizeOfTypeInBits(Type ty) =>
            LLVMSharp.Interop.LLVM.SizeOfTypeInBits(_targetData, (LLVMTypeRef) ty);

        public unsafe ulong StoreSizeOfType(Type ty) => LLVMSharp.Interop.LLVM.StoreSizeOfType(_targetData,
            (LLVMTypeRef) ty);

        public unsafe ulong ABISizeOfType(Type ty) =>
            LLVMSharp.Interop.LLVM.ABISizeOfType(_targetData, (LLVMTypeRef) ty);

        public unsafe uint ABIAlignmentOfType(Type ty) => LLVMSharp.Interop.LLVM.ABIAlignmentOfType(_targetData,
            (LLVMTypeRef) ty);

        public unsafe uint CallFrameAlignmentOfType(Type ty) =>
            LLVMSharp.Interop.LLVM.CallFrameAlignmentOfType(_targetData, (LLVMTypeRef) ty);

        public unsafe uint PreferredAlignmentOfType(Type ty) =>
            LLVMSharp.Interop.LLVM.PreferredAlignmentOfType(_targetData, (LLVMTypeRef) ty);
    }
}
