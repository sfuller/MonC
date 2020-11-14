using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public struct Metadata
    {
        private LLVMMetadataRef _metadata;
        public bool IsValid => _metadata.Handle != IntPtr.Zero;

        public static Metadata Null => new Metadata();

        internal Metadata(LLVMMetadataRef metadata) => _metadata = metadata;

        public static unsafe Metadata FromValue(Value val) =>
            (LLVMMetadataRef) LLVMSharp.Interop.LLVM.ValueAsMetadata((LLVMValueRef) val);

        public static implicit operator LLVMMetadataRef(Metadata metadata) => metadata._metadata;
        public static implicit operator Metadata(LLVMMetadataRef metadata) => new Metadata(metadata);

        public unsafe ulong GetTypeSizeInBits() => IsValid ? LLVMSharp.Interop.LLVM.DITypeGetSizeInBits(_metadata) : 0;

        public unsafe ulong GetTypeOffsetInBits() =>
            IsValid ? LLVMSharp.Interop.LLVM.DITypeGetOffsetInBits(_metadata) : 0;

        public unsafe uint GetTypeAlignInBits() => IsValid ? LLVMSharp.Interop.LLVM.DITypeGetAlignInBits(_metadata) : 0;

        public unsafe void ReplaceAllUsesWith(Metadata replacement) =>
            LLVMSharp.Interop.LLVM.MetadataReplaceAllUsesWith(_metadata, (LLVMMetadataRef) replacement);

        public unsafe void DisposeTemporaryMDNode() => LLVMSharp.Interop.LLVM.DisposeTemporaryMDNode(_metadata);
    }
}
