namespace MonC.LLVM
{
    public struct Type
    {
        private CAPI.LLVMTypeRef _type;
        public bool IsValid => _type.IsValid;

        internal Type(CAPI.LLVMTypeRef type)
        {
            _type = type;
        }
        
        public static implicit operator CAPI.LLVMTypeRef(Type type) => type._type;

        public Type PointerType() => new Type(CAPI.LLVMPointerType(_type, 0));
        public Type ArrayType(uint elementCount) => new Type(CAPI.LLVMArrayType(_type, elementCount));
    }
}