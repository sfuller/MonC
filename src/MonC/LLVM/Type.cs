using System;

namespace MonC.LLVM
{
    public struct Type
    {
        private CAPI.LLVMTypeRef _type;
        public bool IsValid => _type.IsValid;

        public static bool operator ==(Type a, Type b) => a._type == b._type;
        public static bool operator !=(Type a, Type b) => a._type != b._type;

        public override bool Equals(Object obj)
        {
            if (GetType() != obj.GetType())
                return false;
            return this == (Type) obj;
        }

        public override int GetHashCode() => _type.GetHashCode();

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

        public bool IsFirstClassType()
        {
            // Return true if the type is "first class", meaning it is a valid type for a Value
            CAPI.LLVMTypeKind kind = Kind;
            return kind != CAPI.LLVMTypeKind.Function && kind != CAPI.LLVMTypeKind.Void;
        }

        public bool IsFloatingPointTy()
        {
            CAPI.LLVMTypeKind kind = Kind;
            return kind == CAPI.LLVMTypeKind.Half || kind == CAPI.LLVMTypeKind.BFloat ||
                   kind == CAPI.LLVMTypeKind.Float || kind == CAPI.LLVMTypeKind.Double ||
                   kind == CAPI.LLVMTypeKind.X86_FP80 || kind == CAPI.LLVMTypeKind.FP128 ||
                   kind == CAPI.LLVMTypeKind.PPC_FP128;
        }

        public Type ElementType => CAPI.LLVMGetElementType(_type);

        public uint VectorSize => CAPI.LLVMGetVectorSize(_type);

        public uint PointerAddressSpace => CAPI.LLVMGetPointerAddressSpace(_type);

        public uint GetPrimitiveSizeInBits()
        {
            switch (Kind) {
                case CAPI.LLVMTypeKind.Half: return 16;
                case CAPI.LLVMTypeKind.BFloat: return 16;
                case CAPI.LLVMTypeKind.Float: return 32;
                case CAPI.LLVMTypeKind.Double: return 64;
                case CAPI.LLVMTypeKind.X86_FP80: return 80;
                case CAPI.LLVMTypeKind.FP128: return 128;
                case CAPI.LLVMTypeKind.PPC_FP128: return 128;
                case CAPI.LLVMTypeKind.X86_MMX: return 64;
                case CAPI.LLVMTypeKind.Integer: return IntTypeWidth;
                case CAPI.LLVMTypeKind.Vector:
                case CAPI.LLVMTypeKind.ScalableVector:
                    return ElementType.GetPrimitiveSizeInBits() * VectorSize;
                default: return 0;
            }
        }
    }
}
