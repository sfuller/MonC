namespace MonC.LLVM
{
    public struct Metadata
    {
        private CAPI.LLVMMetadataRef _metadata;
        public bool IsValid => _metadata.IsValid;

        public static Metadata Null => new Metadata();

        internal Metadata(CAPI.LLVMMetadataRef metadata)
        {
            _metadata = metadata;
        }

        public static Metadata FromValue(Value val)
        {
            return new Metadata(CAPI.LLVMValueAsMetadata(val));
        }

        public static implicit operator CAPI.LLVMMetadataRef(Metadata metadata) => metadata._metadata;

        public ulong TypeSizeInBits => CAPI.DI.LLVMDITypeGetSizeInBits(_metadata);
        public ulong TypeOffsetInBits => CAPI.DI.LLVMDITypeGetOffsetInBits(_metadata);
        public uint TypeAlignInBits => CAPI.DI.LLVMDITypeGetAlignInBits(_metadata);

        public void ReplaceAllUsesWith(Metadata replacement) =>
            CAPI.LLVMMetadataReplaceAllUsesWith(_metadata, replacement);
    }
}