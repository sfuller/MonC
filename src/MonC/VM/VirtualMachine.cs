using System;
using System.Collections.Generic;
using MonC.IL;

namespace MonC.VM
{
    public class VirtualMachine : IVMBindingContext, IDebuggableVM
    {
        private readonly List<StackFrame> _callStack = new List<StackFrame>();
        private bool _isContinuing;
        private bool _isPaused;
        private bool _isStartingYield;
        private bool _isYielding;
        private readonly StackFrameMemory _argumentBuffer = new StackFrameMemory();
        private Action<bool> _finishedCallback = success => { };
        private IVMDebugger? _debugger;

        private bool _isStepping;
        private int _cycleCount;

        public bool IsRunning { get; private set; }
        public bool IsPaused => _isPaused;
        public byte[] ReturnValueBuffer { get; private set; } = new byte[0];
        public int CallStackFrameCount => _callStack.Count;

        public int MaxCycles { get; set; }

        public VirtualMachine()
        {
            MaxCycles = -1;
        }

        public bool Call(
            VMModule module,
            string functionName,
            byte[] argumentData,
            Action<bool>? finished = null)
        {
            if (IsRunning) {
                throw new InvalidOperationException("Cannot call function while running");
            }

            int functionIndex = LookupFunction(module.ILModule, functionName);

            if (functionIndex == -1) {
                return false;
            }

            int argumentsSize;

            if (functionIndex < module.ILModule.DefinedFunctions.Length) {
                argumentsSize = module.ILModule.DefinedFunctions[functionIndex].ArgumentMemorySize;
            } else {
                argumentsSize = module.VMFunctions[functionIndex].ArgumentMemorySize;
            }

            if (argumentData.Length != argumentsSize) {
                // TODO: Should this be an exception? At the very least we need a result code to determine why it failed.
                return false;
            }

            if (finished == null) {
                finished = success => { };
            }

            IsRunning = true;

            _finishedCallback = finished;
            _cycleCount = 0;

            PushCall(module, functionIndex, argumentData);

            if (_isStepping) {
                Break();
            } else {
                Continue();
            }

            return true;
        }

        public void SetDebugger(IVMDebugger debugger)
        {
            if (_debugger != null) {
                // Don't let the debugger change until we have a way to tell the current debugger to clean up.
                // (Restore the module to it's original state and so on)
                // TODO: It might be acceptable right now to set a new debugger when the VM is not active.
                throw new InvalidOperationException("A debugger has already been set.");
            }

            _debugger = debugger;
        }

        string IVMBindingContext.GetString(int id)
        {
            var strings = PeekCallStack().Module.ILModule.Strings;
            if (id < 0 || id >= strings.Length) {
                return "";
            }
            return strings[id];
        }

        public static int LookupFunction(ILModule module, string functionName)
        {
            for (int i = 0, ilen = module.ExportedFunctions.Length; i < ilen; ++i) {
                KeyValuePair<string, int> exportedFunction = module.ExportedFunctions[i];
                if (exportedFunction.Key == functionName) {
                    return exportedFunction.Value;
                }
            }
            return -1;
        }

        void IDebuggableVM.Continue()
        {
            if (!_isPaused) {
                throw new InvalidOperationException();
            }
            _isPaused = false;
            Continue();
        }

        private void Continue()
        {
            if (_isContinuing) {
                return;
            }

            _isContinuing = true;

            while (_isContinuing) {
                InterpretCurrentInstruction();
            }
        }

        private void HandleYieldComplete()
        {
            _isYielding = false;
            if (!_isStartingYield) {
                Continue();
            }
        }

        void IDebuggableVM.SetStepping(bool isStepping)
        {
            _isStepping = isStepping;
        }

        // TODO: Rename to GetStackFrameInfo
        public StackFrameInfo GetStackFrame(int depth)
        {
            StackFrame frame = GetInternalStackFrame(depth);

            return new StackFrameInfo {
                Module = frame.Module,
                Function = frame.Function,
                PC = frame.PC
            };
        }

        public StackFrameMemory GetStackFrameMemory(int depth)
        {
            StackFrame frame = GetInternalStackFrame(depth);
            return frame.Memory;
        }

        // TODO: Rename to GetStackFrame
        private StackFrame GetInternalStackFrame(int depth)
        {
            if (depth >= _callStack.Count) {
                return new StackFrame();
            }
            return _callStack[_callStack.Count - 1 - depth];
        }

        private StackFrame PeekCallStack()
        {
            return _callStack[_callStack.Count - 1];
        }

        private void PushCallStack(StackFrame frame)
        {
            _callStack.Add(frame);
        }

        private void Break()
        {
            _isPaused = true;
            _isContinuing = false;
            if (_debugger != null) {
                _debugger.HandleBreak();
            }
        }

        private void InterpretCurrentInstruction()
        {
            if (MaxCycles >= 0 && _cycleCount >= MaxCycles) {
                Abort();
                return;
            }
            ++_cycleCount;

            StackFrame top = PeekCallStack();

            bool canBreak;
            bool breakRequested = _isStepping;

            if (top.IsBoundFunction()) {
                canBreak = InterpretBoundFunctionCall(top);
            } else {
                // It is always safe to break between instructions.
                canBreak = true;

                Instruction ins = top.Module.ILModule.DefinedFunctions[top.Function].Code[top.PC];
                ++top.PC;
                breakRequested |= InterpretInstruction(ins);
            }

            if (breakRequested && canBreak && IsRunning) {
                Break();
            }
        }

        private bool InterpretBoundFunctionCall(StackFrame frame)
        {
            VMFunction boundFunction = frame.Module.VMFunctions[frame.Function];
            boundFunction.Delegate(this, new ArgumentSource(frame.Memory, 0));

            PopFrame();

            // Return true to signify that we can break.
            return true;
        }

        private bool InterpretInstruction(Instruction ins)
        {
            // Returns true if a break should be triggered. Otherwise false.
            switch (ins.Op) {
                case OpCode.NOOP:
                    InterpretNoOp();
                    break;
                case OpCode.BREAK:
                    InterpretBreak();
                    return true;
                case OpCode.READ:
                    InterpretRead(ins);
                    break;
                case OpCode.WRITE:
                    InterpretWrite(ins);
                    break;
                case OpCode.PUSHWORD:
                    InterpretPushWord(ins);
                    break;
                case OpCode.PUSH:
                    InterpretPush(ins);
                    break;
                case OpCode.POP:
                    InterpretPop(ins);
                    break;
                case OpCode.ACCESS:
                    InterpretAccess(ins);
                    break;
                case OpCode.CALL:
                    InterpretCall();
                    break;
                case OpCode.RETURN:
                    InterpretReturn();
                    break;
                case OpCode.CMPE:
                    InterpretCmpE();
                    break;
                case OpCode.CMPLT:
                    InterpretCmpLT();
                    break;
                case OpCode.CMPLTE:
                    InterpretCmpLTE();
                    break;
                case OpCode.JUMP:
                    InterpretJump(ins);
                    break;
                case OpCode.JUMPZ:
                    InterpretJumpZ(ins);
                    break;
                case OpCode.JUMPNZ:
                    InterpretJumpNZ(ins);
                    break;
                case OpCode.BOOL:
                    InterpretBool();
                    break;
                case OpCode.LNOT:
                    InterpretLogicalNot();
                    break;
                case OpCode.ADD:
                    InterpretAdd();
                    break;
                case OpCode.SUB:
                    InterpretSub();
                    break;
                case OpCode.AND:
                    InterpretAnd();
                    break;
                case OpCode.OR:
                    InterpretOr();
                    break;
                case OpCode.MUL:
                    InterpretMul();
                    break;
                case OpCode.DIV:
                    InterpretDiv();
                    break;
                case OpCode.MOD:
                    InterpretMod();
                    break;
                default:
                    throw new NotImplementedException();
            }

            // All instructions but break return false to signify that break did not occur.
            return false;
        }

        private void InterpretNoOp()
        {
        }

        private void InterpretBreak()
        {
            --PeekCallStack().PC;
            // Note: Triggering the actual break is done by InterpretInstruction.
        }

        private void InterpretRead(Instruction ins)
        {
            StackFrame frame = PeekCallStack();
            for (int i = 0; i < ins.SizeValue; ++i) {
                frame.Memory.PushVal(frame.Memory.Read(ins.ImmediateValue + i));
            }
        }

        private void InterpretWrite(Instruction ins)
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int sourcePointer = stack.StackPointer - ins.SizeValue;

            for (int i = 0; i < ins.SizeValue; ++i) {
                stack.Write(ins.ImmediateValue + i, stack.Read(sourcePointer++));
            }
        }

        private void InterpretPushWord(Instruction ins)
        {
            PeekCallStack().Memory.PushValInt(ins.ImmediateValue);
        }

        private void InterpretPush(Instruction ins)
        {
            PeekCallStack().Memory.Push(ins.SizeValue);
        }

        private void InterpretPop(Instruction ins)
        {
            PeekCallStack().Memory.Discard(ins.SizeValue);
        }

        private void InterpretAccess(Instruction ins)
        {
            PeekCallStack().Memory.Access(ins.ImmediateValue, ins.SizeValue);
        }

        private void InterpretCall()
        {
            StackFrame currentFrame = PeekCallStack();
            PushCall(currentFrame.Module, currentFrame.Memory);
        }

        private void InterpretReturn()
        {
            PopFrame();
        }

        private void InterpretCmpE()
        {
            StackFrame frame = PeekCallStack();
            int a = frame.Memory.PopValInt();
            int b = frame.Memory.PopValInt();
            frame.Memory.PushValInt(a == b ? 1 : 0);
        }

        private void InterpretCmpLT()
        {
            StackFrame frame = PeekCallStack();
            int a = frame.Memory.PopValInt();
            int b = frame.Memory.PopValInt();
            frame.Memory.PushValInt(a < b ? 1 : 0);
        }

        private void InterpretCmpLTE()
        {
            StackFrame frame = PeekCallStack();
            int a = frame.Memory.PopValInt();
            int b = frame.Memory.PopValInt();
            frame.Memory.PushValInt(a <= b ? 1 : 0);
        }

        private void InterpretJump(Instruction ins)
        {
            Jump(ins.ImmediateValue);
        }

        private void InterpretJumpZ(Instruction ins)
        {
            StackFrame frame = PeekCallStack();
            if (frame.Memory.PopValInt() == 0) {
                Jump(ins.ImmediateValue);
            }
        }

        private void InterpretJumpNZ(Instruction ins)
        {
            StackFrame frame = PeekCallStack();
            if (frame.Memory.PopValInt() != 0) {
                Jump(ins.ImmediateValue);
            }
        }

        private void InterpretBool()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int val = stack.PopValInt();
            stack.PushValInt(val == 0 ? 0 : 1);
        }

        private void InterpretLogicalNot()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int val = stack.PopValInt();
            stack.PushValInt(val == 0 ? 1 : 0);
        }

        private void InterpretAdd()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a + b);
        }

        private void InterpretSub()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a - b);
        }

        private void InterpretAnd()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a & b);
        }

        private void InterpretOr()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a | b);
        }

        private void InterpretMul()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a * b);
        }

        private void InterpretDiv()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a / b);
        }

        private void InterpretMod()
        {
            StackFrameMemory stack = PeekCallStack().Memory;
            int a = stack.PopValInt();
            int b = stack.PopValInt();
            stack.PushValInt(a % b);
        }

        private void Jump(int addr)
        {
            PeekCallStack().PC = addr;
        }

        private void PushCall(VMModule module, StackFrameMemory sourceStack)
        {
            int functionIndex = sourceStack.PopValInt();

            GetFunctionStackSizes(module, functionIndex,
                out int returnValueSize,
                out int argumentMemorySize,
                out int stackSize);

            StackFrame frame = AcquireFrame(stackSize);
            frame.Module = module;
            frame.Function = functionIndex;
            frame.Memory.CopyFrom(sourceStack, sourceStack.StackPointer - argumentMemorySize, returnValueSize, argumentMemorySize);
            sourceStack.Discard(argumentMemorySize);
            PushCallStack(frame);

            // TODO: PERF: Try to optimize out calls to HandleModuleAdded when we can deduce that the module has already
            // been seen.
            // For calls pushed by the CALL instruction, we know that the module will be the same and that we don't need
            // to call this function.
            _debugger?.HandleModuleAdded(module);
        }

        private void GetFunctionStackSizes(
                VMModule module,
                int functionIndex,
                out int returnValueSize,
                out int argumentMemorySize,
                out int stackSize)
        {
            int definedFunctionCount = module.ILModule.DefinedFunctions.Length;

            if (functionIndex >= definedFunctionCount) {
                VMFunction function = module.VMFunctions[functionIndex];
                returnValueSize = function.ReturnValueSize;
                argumentMemorySize = function.ArgumentMemorySize;
                stackSize = returnValueSize + argumentMemorySize;
                //frame = AcquireFrame(function.ArgumentMemorySize);
                //frame.BindingEnumerator = function.Delegate(this, new ArgumentSource(frame.Memory, 0));
            } else {
                ILFunction function = module.ILModule.DefinedFunctions[functionIndex];
                returnValueSize = function.ReturnValueSize;
                argumentMemorySize = function.ArgumentMemorySize;
                stackSize = function.MaxStackSize;
                //frame = AcquireFrame(function.MaxStackSize);
            }
        }

        private void PushCall(VMModule module, int functionIndex, byte[] argumentData)
        {
            // TODO: Need either better documentation about how bound functions must not re-retrieve arguments from
            // the original argumentSource after a Call Continuation, or we need to use unique argument buffers for each
            // bound function call (with pooling of course). I'd opt for the former, it's super easy to just grab your
            // arguments as soon as you enter the bound function, and is the cleanest as well.

            GetFunctionStackSizes(module, functionIndex,
                out int _,
                out int argumentMemorySize,
                out int stackSize);

            if (argumentData.Length != argumentMemorySize) {
                throw new ArgumentOutOfRangeException(nameof(argumentData), "Function argument size differs.");
            }

            // Function input + space for function index
            _argumentBuffer.Recreate(stackSize + sizeof(int));

            for (int i = 0, ilen = argumentData.Length; i < ilen; ++i) {
                _argumentBuffer.PushVal(argumentData[i]);
            }
            _argumentBuffer.PushValInt(functionIndex);
            PushCall(module, _argumentBuffer);
        }

        private StackFrame AcquireFrame(int memorySize)
        {
            StackFrame frame;
            if (_framePool.Count > 0) {
                frame = _framePool.Pop();
            } else {
                frame = new StackFrame();
            }

            frame.Memory.Recreate(memorySize);
            return frame;
        }

        private void PopFrame()
        {
            int top = _callStack.Count - 1;
            StackFrame frame = _callStack[top];
            _callStack.RemoveAt(top);

            frame.PC = 0;
            _framePool.Push(frame);

            GetFunctionStackSizes(frame.Module, frame.Function, out int returnValueSize, out int _, out int _);

            if (_callStack.Count == 0) {
                if (ReturnValueBuffer.Length < returnValueSize) {
                    ReturnValueBuffer = new byte[returnValueSize];
                }
                frame.Memory.CopyTo(0, 0, returnValueSize, ReturnValueBuffer);

                _isContinuing = false;
                HandleFinished(true);
            } else {
                StackFrame returnFrame = PeekCallStack();
                returnFrame.Memory.PushFrom(frame.Memory, 0, returnValueSize);
            }
        }

        private readonly Stack<StackFrame> _framePool = new Stack<StackFrame>();

        private void Abort()
        {
            _callStack.Clear();
            _isContinuing = false;
            HandleFinished(false);
        }

        private void HandleFinished(bool success)
        {
            IsRunning = false;
            if (_debugger != null) {
                _debugger.HandleFinished();
            }
            _finishedCallback(success);
        }
    }
}
