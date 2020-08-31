using System;

namespace MonC.LLVM
{
    public struct Type
    {
        private CAPI.LLVMTypeRef _type;
        public bool IsValid => _type.IsValid;

        internal Type(CAPI.LLVMTypeRef type) => _type = type;

        public static implicit operator CAPI.LLVMTypeRef(Type type) => type._type;
        public static implicit operator Type(CAPI.LLVMTypeRef type) => new Type(type);

        public CAPI.LLVMTypeKind Kind => CAPI.LLVMGetTypeKind(_type);

        public Type PointerType() => CAPI.LLVMPointerType(_type, 0);
        public Type ArrayType(uint elementCount) => CAPI.LLVMArrayType(_type, elementCount);

        public bool IsFunctionVarArg => CAPI.LLVMIsFunctionVarArg(_type);
        public Type ReturnType => CAPI.LLVMGetReturnType(_type);
        public uint NumParamTypes => CAPI.LLVMCountParamTypes(_type);
        public Type[] ParamTypes => Array.ConvertAll(CAPI.LLVMGetParamTypes(_type), tp => (Type) tp);

        public uint IntTypeWidth => CAPI.LLVMGetIntTypeWidth(_type);
    }
}