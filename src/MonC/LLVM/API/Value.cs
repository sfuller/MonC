using System;

namespace MonC.LLVM
{
    public struct Value
    {
        private CAPI.LLVMValueRef _value;
        public bool IsValid => _value.IsValid;

        internal Value(CAPI.LLVMValueRef value) => _value = value;

        public static implicit operator CAPI.LLVMValueRef(Value value) => value._value;
        public static implicit operator Value(CAPI.LLVMValueRef value) => new Value(value);

        public CAPI.LLVMValueKind Kind => IsValid ? CAPI.LLVMGetValueKind(_value) : CAPI.LLVMValueKind.Invalid;

        public bool IsGlobalValue()
        {
            CAPI.LLVMValueKind kind = Kind;
            return kind == CAPI.LLVMValueKind.Function ||
                   kind == CAPI.LLVMValueKind.GlobalAlias ||
                   kind == CAPI.LLVMValueKind.GlobalIFunc ||
                   kind == CAPI.LLVMValueKind.GlobalVariable;
        }

        public Type TypeOf => IsValid ? CAPI.LLVMTypeOf(_value) : new CAPI.LLVMTypeRef();

        public static Value ConstInt(Type intTy, ulong n, bool signExtend) => CAPI.LLVMConstInt(intTy, n, signExtend);

        public static Value ConstReal(Type fltTy, double n) => CAPI.LLVMConstReal(fltTy, n);

        public BasicBlock FirstBasicBlock =>
            IsValid && Kind == CAPI.LLVMValueKind.Function
                ? CAPI.LLVMGetFirstBasicBlock(_value)
                : new CAPI.LLVMBasicBlockRef();

        public BasicBlock LastBasicBlock => IsValid && Kind == CAPI.LLVMValueKind.Function
            ? CAPI.LLVMGetLastBasicBlock(_value)
            : new CAPI.LLVMBasicBlockRef();

        public CAPI.LLVMOpcode InstructionOpcode =>
            IsValid && Kind == CAPI.LLVMValueKind.Instruction
                ? CAPI.LLVMGetInstructionOpcode(_value)
                : CAPI.LLVMOpcode.Invalid;

        public void SetFuncSubprogram(Metadata sp) => CAPI.LLVMSetSubprogram(_value, sp);

        public bool IsDeclaration => IsValid && IsGlobalValue() && CAPI.LLVMIsDeclaration(_value);
        public bool IsUndef => IsValid && CAPI.LLVMIsUndef(_value);

        public CAPI.LLVMLinkage Linkage =>
            IsValid && IsGlobalValue() ? CAPI.LLVMGetLinkage(_value) : CAPI.LLVMLinkage.External;

        public void SetLinkage(CAPI.LLVMLinkage linkage) => CAPI.LLVMSetLinkage(_value, linkage);

        public string Name => IsValid ? CAPI.LLVMGetValueName2(_value) : string.Empty;
        public void SetName(string name) => CAPI.LLVMSetValueName2(_value, name);

        public uint NumParams => IsValid && Kind == CAPI.LLVMValueKind.Function ? CAPI.LLVMCountParams(_value) : 0;

        public Value[] Params =>
            IsValid && Kind == CAPI.LLVMValueKind.Function
                ? Array.ConvertAll(CAPI.LLVMGetParams(_value), param => (Value) param)
                : new Value[] { };

        public void AddIncoming(Value[] incomingValues, BasicBlock[] incomingBlocks) =>
            CAPI.LLVMAddIncoming(_value, Array.ConvertAll(incomingValues, val => (CAPI.LLVMValueRef) val),
                Array.ConvertAll(incomingBlocks, block => (CAPI.LLVMBasicBlockRef) block));

        public void AppendExistingBasicBlock(BasicBlock bb) => CAPI.LLVMAppendExistingBasicBlock(_value, bb);
    }
}
