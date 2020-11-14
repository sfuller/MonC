using System;
using LLVMSharp.Interop;

namespace MonC.LLVM
{
    public struct Value
    {
        private LLVMValueRef _value;
        public bool IsValid => _value.Handle != IntPtr.Zero;

        internal Value(LLVMValueRef value) => _value = value;

        public static implicit operator LLVMValueRef(Value value) => value._value;
        public static implicit operator Value(LLVMValueRef value) => new Value(value);

        public LLVMValueKind Kind => _value.Kind;

        public bool IsGlobalValue()
        {
            LLVMValueKind kind = Kind;
            return kind == LLVMValueKind.LLVMFunctionValueKind || kind == LLVMValueKind.LLVMGlobalAliasValueKind ||
                   kind == LLVMValueKind.LLVMGlobalIFuncValueKind || kind == LLVMValueKind.LLVMGlobalVariableValueKind;
        }

        public Type TypeOf => _value.TypeOf;

        public static Value ConstInt(Type intTy, ulong n, bool signExtend) =>
            LLVMValueRef.CreateConstInt(intTy, n, signExtend);

        public static Value ConstReal(Type fltTy, double n) => LLVMValueRef.CreateConstReal(fltTy, n);

        public BasicBlock FirstBasicBlock => _value.FirstBasicBlock;

        public BasicBlock LastBasicBlock => _value.LastBasicBlock;

        public LLVMOpcode InstructionOpcode => _value.InstructionOpcode;

        public unsafe void SetFuncSubprogram(Metadata sp) =>
            LLVMSharp.Interop.LLVM.SetSubprogram(_value, (LLVMMetadataRef) sp);

        public bool IsDeclaration => _value.IsDeclaration;
        public bool IsUndef => _value.IsUndef;

        public LLVMLinkage Linkage => _value.Linkage;

        public void SetLinkage(LLVMLinkage linkage) => _value.Linkage = linkage;

        public string Name => _value.Name;
        public void SetName(string name) => _value.Name = name;

        public uint NumParams => _value.ParamsCount;

        public Value[] Params =>
            IsValid && Kind == LLVMValueKind.LLVMFunctionValueKind
                ? Array.ConvertAll(_value.Params, param => (Value) param)
                : new Value[] { };

        public void AddIncoming(Value[] incomingValues, BasicBlock[] incomingBlocks) =>
            _value.AddIncoming(Array.ConvertAll(incomingValues, val => (LLVMValueRef) val),
                Array.ConvertAll(incomingBlocks, block => (LLVMBasicBlockRef) block), (uint) incomingValues.Length);

        public unsafe void AppendExistingBasicBlock(BasicBlock bb) =>
            LLVMSharp.Interop.LLVM.AppendExistingBasicBlock(_value, (LLVMBasicBlockRef) bb);
    }
}
