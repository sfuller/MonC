using System;
using System.Collections.Generic;
using MonC.IL;

namespace MonC.VM
{
    public class VirtualMachine : IVMBindingContext, IDebuggableVM
    {
        private readonly List<StackFrame> _callStack = new List<StackFrame>();
        //private VMModule _module = new VMModule();
        private int _aRegister;
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
        public int ReturnValue => _aRegister;
        public int CallStackFrameCount => _callStack.Count;

        public int MaxCycles { get; set; }

        public VirtualMachine()
        {
            MaxCycles = -1;
        }

        // public void LoadModule(VMModule module)
        // {
        //     if (IsRunning) {
        //         throw new InvalidOperationException("Cannot load module while running");
        //     }
        //     _module = module;
        // }

        public bool Call(
            VMModule module,
            string functionName,
            IReadOnlyList<int> arguments,
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

            if (arguments.Count != argumentsSize) {
                return false;
            }

            if (finished == null) {
                finished = success => { };
            }

            IsRunning = true;

            _finishedCallback = finished;
            _cycleCount = 0;

            PushCall(module, functionIndex, arguments);

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

        /// <summary>
        /// Common operation for instructions which load a value from the stack based on the immediate value of the
        /// instruction.
        /// </summary>
        private int ReadStackWithImmediateValue(Instruction ins)
        {
            return PeekCallStack().Memory.Read(ins.ImmediateValue);
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
                case OpCode.LOAD:
                    InterpretLoad(ins);
                    break;
                case OpCode.READ:
                    InterpretRead(ins);
                    break;
                case OpCode.WRITE:
                    InterpretWrite(ins);
                    break;
                case OpCode.CALL:
                    InterpretCall(ins);
                    break;
                case OpCode.RETURN:
                    InterpretReturn();
                    break;
                case OpCode.CMPE:
                    InterpretCmpE(ins);
                    break;
                case OpCode.CMPLT:
                    InterpretCmpLT(ins);
                    break;
                case OpCode.CMPLTE:
                    InterpretCmpLTE(ins);
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
                    InterpretAdd(ins);
                    break;
                case OpCode.SUB:
                    InterpretSub(ins);
                    break;
                case OpCode.AND:
                    InterpretAnd(ins);
                    break;
                case OpCode.OR:
                    InterpretOr(ins);
                    break;
                case OpCode.MUL:
                    InterpretMul(ins);
                    break;
                case OpCode.DIV:
                    InterpretDiv(ins);
                    break;
                case OpCode.MOD:
                    InterpretMod(ins);
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

        private void InterpretLoad(Instruction ins)
        {
            _aRegister = ins.ImmediateValue;
        }

        private void InterpretRead(Instruction ins)
        {
            _aRegister = ReadStackWithImmediateValue(ins);
        }

        private void InterpretWrite(Instruction ins)
        {
            PeekCallStack().Memory.Write(ins.ImmediateValue, _aRegister);
        }

        private void InterpretCall(Instruction ins)
        {
            StackFrame currentFrame = PeekCallStack();
            PushCall(currentFrame.Module, currentFrame.Memory, ins.ImmediateValue);
        }

        private void InterpretReturn()
        {
            PopFrame();
        }

        private void InterpretCmpE(Instruction ins)
        {
            _aRegister = _aRegister == ReadStackWithImmediateValue(ins) ? 1 : 0;
        }

        private void InterpretCmpLT(Instruction ins)
        {
            _aRegister = _aRegister < ReadStackWithImmediateValue(ins) ? 1 : 0;
        }

        private void InterpretCmpLTE(Instruction ins)
        {
            _aRegister = _aRegister <= ReadStackWithImmediateValue(ins) ? 1 : 0;
        }

        private void InterpretJump(Instruction ins)
        {
            Jump(ins.ImmediateValue);
        }

        private void InterpretJumpZ(Instruction ins)
        {
            if (_aRegister == 0) {
                Jump(ins.ImmediateValue);
            }
        }

        private void InterpretJumpNZ(Instruction ins)
        {
            if (_aRegister != 0) {
                Jump(ins.ImmediateValue);
            }
        }

        private void InterpretBool()
        {
            _aRegister = _aRegister == 0 ? 0 : 1;
        }

        private void InterpretLogicalNot()
        {
            _aRegister = _aRegister == 0 ? 1 : 0;
        }

        private void InterpretAdd(Instruction ins)
        {
            _aRegister += ReadStackWithImmediateValue(ins);
        }

        private void InterpretSub(Instruction ins)
        {
            _aRegister -= ReadStackWithImmediateValue(ins);
        }

        private void InterpretAnd(Instruction ins)
        {
            _aRegister &= ReadStackWithImmediateValue(ins);
        }

        private void InterpretOr(Instruction ins)
        {
            _aRegister |= ReadStackWithImmediateValue(ins);
        }

        private void InterpretMul(Instruction ins)
        {
            _aRegister *= ReadStackWithImmediateValue(ins);
        }

        private void InterpretDiv(Instruction ins)
        {
            _aRegister /= ReadStackWithImmediateValue(ins);
        }

        private void InterpretMod(Instruction ins)
        {
            _aRegister %= ReadStackWithImmediateValue(ins);
        }

        private void Jump(int offset)
        {
            PeekCallStack().PC += offset;
        }

        private void PushCall(VMModule module, StackFrameMemory argumentStackSource, int argumentStackStart)
        {
            // First value on the argument stack is the function index
            int functionIndex = argumentStackSource.Read(argumentStackStart);

            // The rest of the data on the argument stack is argument values
            int argumentValuesStart = argumentStackStart + 1;

            int argumentMemorySize;
            StackFrame frame;

            int definedFunctionCount = module.ILModule.DefinedFunctions.Length;

            if (functionIndex >= definedFunctionCount) {
                VMFunction function = module.VMFunctions[functionIndex];
                argumentMemorySize = function.ArgumentMemorySize;
                frame = AcquireFrame(function.ArgumentMemorySize);
                //frame.BindingEnumerator = function.Delegate(this, new ArgumentSource(frame.Memory, 0));
            } else {
                ILFunction function = module.ILModule.DefinedFunctions[functionIndex];
                argumentMemorySize = function.ArgumentMemorySize;
                frame = AcquireFrame(function.MaxStackSize);
            }

            frame.Module = module;
            frame.Function = functionIndex;
            frame.Memory.CopyFrom(argumentStackSource, argumentValuesStart, 0, argumentMemorySize);
            PushCallStack(frame);

            // TODO: PERF: Try to optimize out calls to HandleModuleAdded when we can deduce that the module has already
            // been seen.
            // For calls pushed by the CALL instruction, we know that the module will be the same and that we don't need
            // to call this function.
            _debugger?.HandleModuleAdded(module);
        }

        private void PushCall(VMModule module, int functionIndex, IReadOnlyList<int> arguments)
        {
            // TODO: Need either better documentation about how bound functions must not re-retrieve arguments from
            // the original argumentSource after a Call Continuation, or we need to use unique argument buffers for each
            // bound function call (with pooling of course). I'd opt for the former, it's super easy to just grab your
            // arguments as soon as you enter the bound function, and is the cleanest as well.

            int argumentCount = arguments.Count;
            _argumentBuffer.Recreate(argumentCount + 1);

            // First value on the argument stack is function index
            _argumentBuffer.Write(0, functionIndex);

            // Rest of the argument stack is argument values.
            for (int i = 0; i < argumentCount; ++i) {
                _argumentBuffer.Write(i + 1, arguments[i]);
            }

            PushCall(module, _argumentBuffer, 0);
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

            if (_callStack.Count == 0) {
                _isContinuing = false;
                HandleFinished(true);
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
