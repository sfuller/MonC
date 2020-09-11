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

        public CAPI.LLVMValueKind Kind => CAPI.LLVMGetValueKind(_value);

        public Type TypeOf => CAPI.LLVMTypeOf(_value);

        public static Value ConstInt(Type intTy, ulong n, bool signExtend) => CAPI.LLVMConstInt(intTy, n, signExtend);

        public static Value ConstReal(Type fltTy, double n) => CAPI.LLVMConstReal(fltTy, n);

        public BasicBlock FirstBasicBlock => CAPI.LLVMGetFirstBasicBlock(_value);
        public BasicBlock LastBasicBlock => CAPI.LLVMGetLastBasicBlock(_value);

        public CAPI.LLVMOpcode InstructionOpcode => CAPI.LLVMGetInstructionOpcode(_value);

        public void SetFuncSubprogram(Metadata sp) => CAPI.LLVMSetSubprogram(_value, sp);

        public bool IsDeclaration => CAPI.LLVMIsDeclaration(_value);
        public bool IsUndef => CAPI.LLVMIsUndef(_value);

        public CAPI.LLVMLinkage Linkage => CAPI.LLVMGetLinkage(_value);
        public void SetLinkage(CAPI.LLVMLinkage linkage) => CAPI.LLVMSetLinkage(_value, linkage);

        public string Name => CAPI.LLVMGetValueName2(_value);
        public void SetName(string name) => CAPI.LLVMSetValueName2(_value, name);

        public uint NumParams => CAPI.LLVMCountParams(_value);
        public Value[] Params => Array.ConvertAll(CAPI.LLVMGetParams(_value), param => (Value) param);

        public void AddIncoming(Value[] incomingValues, BasicBlock[] incomingBlocks) =>
            CAPI.LLVMAddIncoming(_value, Array.ConvertAll(incomingValues, val => (CAPI.LLVMValueRef) val),
                Array.ConvertAll(incomingBlocks, block => (CAPI.LLVMBasicBlockRef) block));

        public void AppendExistingBasicBlock(BasicBlock bb) => CAPI.LLVMAppendExistingBasicBlock(_value, bb);
    }
}
