using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public struct Type
    {
        private LLVMTypeRef _type;
        public bool IsValid => _type.Handle != IntPtr.Zero;

        public static bool operator ==(Type a, Type b) => a._type == b._type;
        public static bool operator !=(Type a, Type b) => a._type != b._type;

        public override bool Equals(Object obj)
        {
            if (GetType() != obj.GetType())
                return false;
            return this == (Type) obj;
        }

        public override int GetHashCode() => _type.GetHashCode();

        internal Type(LLVMTypeRef type) => _type = type;

        public static implicit operator LLVMTypeRef(Type type) => type._type;
        public static implicit operator Type(LLVMTypeRef type) => new Type(type);

        public LLVMTypeKind Kind => _type.Kind;

        public Type PointerType() => LLVMTypeRef.CreatePointer(_type, 0);
        public Type ArrayType(uint elementCount) => LLVMTypeRef.CreateArray(_type, elementCount);

        public bool IsFunctionVarArg => _type.IsFunctionVarArg;

        public Type ReturnType => _type.ReturnType;

        public uint NumParamTypes => _type.ParamTypesCount;

        public Type[] ParamTypes =>
            IsValid && Kind == LLVMTypeKind.LLVMFunctionTypeKind
                ? Array.ConvertAll(_type.ParamTypes, tp => (Type) tp)
                : new Type[] { };

        public uint IntTypeWidth => _type.IntWidth;

        public bool IsFirstClassType()
        {
            // Return true if the type is "first class", meaning it is a valid type for a Value
            LLVMTypeKind kind = Kind;
            return kind != LLVMTypeKind.LLVMFunctionTypeKind && kind != LLVMTypeKind.LLVMVoidTypeKind;
        }

        public bool IsFloatingPointType()
        {
            LLVMTypeKind kind = Kind;
            return kind == LLVMTypeKind.LLVMHalfTypeKind || kind == LLVMTypeKind.LLVMFloatTypeKind ||
                   kind == LLVMTypeKind.LLVMDoubleTypeKind || kind == LLVMTypeKind.LLVMX86_FP80TypeKind ||
                   kind == LLVMTypeKind.LLVMFP128TypeKind || kind == LLVMTypeKind.LLVMPPC_FP128TypeKind;
        }

        public bool IsVectorType()
        {
            LLVMTypeKind kind = Kind;
            return kind == LLVMTypeKind.LLVMVectorTypeKind;
        }

        public bool IsStructType()
        {
            return Kind == LLVMTypeKind.LLVMStructTypeKind;
        }

        public bool HasElements()
        {
            LLVMTypeKind kind = Kind;
            return kind == LLVMTypeKind.LLVMPointerTypeKind || kind == LLVMTypeKind.LLVMArrayTypeKind ||
                   kind == LLVMTypeKind.LLVMVectorTypeKind;
        }

        public Type ElementType => _type.ElementType;

        public uint VectorSize => _type.VectorSize;

        public uint PointerAddressSpace => _type.PointerAddressSpace;

        public uint GetPrimitiveSizeInBits()
        {
            switch (Kind) {
                case LLVMTypeKind.LLVMHalfTypeKind: return 16;
                case LLVMTypeKind.LLVMFloatTypeKind: return 32;
                case LLVMTypeKind.LLVMDoubleTypeKind: return 64;
                case LLVMTypeKind.LLVMX86_FP80TypeKind: return 80;
                case LLVMTypeKind.LLVMFP128TypeKind: return 128;
                case LLVMTypeKind.LLVMPPC_FP128TypeKind: return 128;
                case LLVMTypeKind.LLVMX86_MMXTypeKind: return 64;
                case LLVMTypeKind.LLVMIntegerTypeKind: return IntTypeWidth;
                case LLVMTypeKind.LLVMVectorTypeKind:
                    return ElementType.GetPrimitiveSizeInBits() * VectorSize;
                default: return 0;
            }
        }
    }
}
