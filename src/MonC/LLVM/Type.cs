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

        public CAPI.LLVMTypeKind Kind => IsValid ? CAPI.LLVMGetTypeKind(_type) : CAPI.LLVMTypeKind.Void;

        public Type PointerType() => CAPI.LLVMPointerType(_type, 0);
        public Type ArrayType(uint elementCount) => CAPI.LLVMArrayType(_type, elementCount);

        public bool IsFunctionVarArg =>
            IsValid && Kind == CAPI.LLVMTypeKind.Function && CAPI.LLVMIsFunctionVarArg(_type);

        public Type ReturnType => IsValid && Kind == CAPI.LLVMTypeKind.Function
            ? CAPI.LLVMGetReturnType(_type)
            : new CAPI.LLVMTypeRef();

        public uint NumParamTypes =>
            IsValid && Kind == CAPI.LLVMTypeKind.Function ? CAPI.LLVMCountParamTypes(_type) : 0;

        public Type[] ParamTypes =>
            IsValid && Kind == CAPI.LLVMTypeKind.Function
                ? Array.ConvertAll(CAPI.LLVMGetParamTypes(_type), tp => (Type) tp)
                : new Type[] { };

        public uint IntTypeWidth => IsValid && Kind == CAPI.LLVMTypeKind.Integer ? CAPI.LLVMGetIntTypeWidth(_type) : 0;

        public bool IsFirstClassType()
        {
            // Return true if the type is "first class", meaning it is a valid type for a Value
            CAPI.LLVMTypeKind kind = Kind;
            return kind != CAPI.LLVMTypeKind.Function && kind != CAPI.LLVMTypeKind.Void;
        }

        public bool IsFloatingPointType()
        {
            CAPI.LLVMTypeKind kind = Kind;
            return kind == CAPI.LLVMTypeKind.Half || kind == CAPI.LLVMTypeKind.BFloat ||
                   kind == CAPI.LLVMTypeKind.Float || kind == CAPI.LLVMTypeKind.Double ||
                   kind == CAPI.LLVMTypeKind.X86_FP80 || kind == CAPI.LLVMTypeKind.FP128 ||
                   kind == CAPI.LLVMTypeKind.PPC_FP128;
        }

        public bool IsVectorType()
        {
            CAPI.LLVMTypeKind kind = Kind;
            return kind == CAPI.LLVMTypeKind.Vector || kind == CAPI.LLVMTypeKind.ScalableVector;
        }

        public bool HasElements()
        {
            CAPI.LLVMTypeKind kind = Kind;
            return kind == CAPI.LLVMTypeKind.Pointer || kind == CAPI.LLVMTypeKind.Array ||
                   kind == CAPI.LLVMTypeKind.Vector || kind == CAPI.LLVMTypeKind.ScalableVector;
        }

        public Type ElementType => IsValid && HasElements() ? CAPI.LLVMGetElementType(_type) : new CAPI.LLVMTypeRef();

        public uint VectorSize => IsValid && IsVectorType() ? CAPI.LLVMGetVectorSize(_type) : 0;

        public uint PointerAddressSpace =>
            IsValid && Kind == CAPI.LLVMTypeKind.Pointer ? CAPI.LLVMGetPointerAddressSpace(_type) : 0;

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
