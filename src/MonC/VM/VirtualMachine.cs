using System;
using System.Collections.Generic;
using System.Linq;
using MonC.Bytecode;
using MonC.Codegen;

namespace MonC.VM
{
    public class VirtualMachine : IVMBindingContext
    {
        private readonly List<StackFrame> _callStack = new List<StackFrame>();
        private VMModule _module = new VMModule();
        private int _aRegister;
        private bool _isContinuing;
        private readonly StackFrameMemory _argumentBuffer = new StackFrameMemory();
        private Action<bool> _finishedCallback = success => { };
        private Action? _breakHandler;
        private bool _isStepping;
        private int _cycleCount;
        
        public bool IsRunning => _callStack.Count != 0;
        public int ReturnValue => _aRegister;
        public int CallStackFrameCount => _callStack.Count;
        
        public int MaxCycles { get; set; }

        public VirtualMachine()
        {
            MaxCycles = -1;
        }
        
        public void LoadModule(VMModule module)
        {
            if (IsRunning) {
                throw new InvalidOperationException("Cannot load module while running");
            }
            _module = module;
        }

        public bool Call(
            string functionName,
            IReadOnlyList<int> arguments,
            Action<bool>? finished = null,
            bool start = true)
        {
            if (IsRunning) {
                throw new InvalidOperationException("Cannot call function while running");
            }
            
            int functionIndex = LookupFunction(functionName);

            if (functionIndex == -1) {
                return false;
            }
            
            int argumentsSize;
            
            if (functionIndex < _module.Module.DefinedFunctions.Length) {
                argumentsSize = _module.Module.DefinedFunctions[functionIndex].ArgumentMemorySize;
            } else {
                argumentsSize = _module.VMFunctions[functionIndex].ArgumentMemorySize;
            }

            if (arguments.Count != argumentsSize) {
                return false;
            }

            if (finished == null) {
                finished = success => { };
            }

            _finishedCallback = finished;
            _cycleCount = 0;
            
            PushCall(functionIndex, arguments);

            if (start) {
                Continue();    
            }

            return true;
        }

        public void SetBreakHandler(Action handler)
        {
            _breakHandler = handler;
        }

        string IVMBindingContext.GetString(int id)
        {
            var strings = _module.Module.Strings;
            if (id < 0 || id >= strings.Length) {
                return "";
            }
            return strings[id];
        }

        private int LookupFunction(string functionName)
        {
            for (int i = 0, ilen = _module.Module.ExportedFunctions.Length; i < ilen; ++i) {
                KeyValuePair<string, int> exportedFunction = _module.Module.ExportedFunctions[i];
                if (exportedFunction.Key == functionName) {
                    return exportedFunction.Value;
                }
            }
            return -1;
        }
        
        public void Continue()
        {
            if (_isContinuing) {
                return;
            }
            
            _isContinuing = true;
            
            while (_isContinuing) {
                InterpretCurrentInstruction();
            }
        }

        public void SetStepping(bool isStepping)
        {
            _isStepping = isStepping;
        }

        // TODO: Rename to GetStackFrameInfo
        public StackFrameInfo GetStackFrame(int depth)
        {
            StackFrame frame = GetInternalStackFrame(depth);
            
            return new StackFrameInfo {
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

        private StackFrame PopCallStack()
        {
            int top = _callStack.Count - 1;
            StackFrame frame = _callStack[top];
            _callStack.RemoveAt(top);
            return frame;
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
            _isContinuing = false;
            if (_breakHandler != null) {
                _breakHandler();
            }
        }

        private void InterpretCurrentInstruction()
        {
            if (_callStack.Count == 0) {
                _isContinuing = false;
                _finishedCallback(true);
                return;
            }
            
            if (MaxCycles >= 0 && _cycleCount >= MaxCycles) {
                Abort();
                return;
            }
            ++_cycleCount;
            
            StackFrame top = PeekCallStack();

            if (top.BindingEnumerator != null) {
                InterpretBoundFunctionCall(top);
                return;
            }

            Instruction ins = _module.Module.DefinedFunctions[top.Function].Code[top.PC];
            ++top.PC;
            InterpretInstruction(ins);

            if (_isStepping) {
                Break();
            }
        }

        private void InterpretBoundFunctionCall(StackFrame frame)
        {
            // This method should only be called after checking that a binding enumerator exists on the given frame.
            IEnumerator<Continuation> bindingEnumerator = frame.BindingEnumerator!;
            
            if (!bindingEnumerator.MoveNext()) {
                // Function has finished
                PopFrame();
                return;
            }

            Continuation continuation = bindingEnumerator.Current;

            if (continuation.Action == ContinuationAction.CALL) {
                PushCall(continuation.FunctionIndex, continuation.Arguments);
                return;
            }

            if (continuation.Action == ContinuationAction.RETURN) {
                _aRegister = continuation.ReturnValue;
                PopFrame();
                return;
            }

            if (continuation.Action == ContinuationAction.YIELD) {
                _isContinuing = false;
                continuation.YieldToken.OnFinished(Continue);

                // Remember: Caling Start can call the finish callback, so call Start at the very end.
                continuation.YieldToken.Start();
                
                return;
            }

            if (continuation.Action == ContinuationAction.UNWRAP) {
                StackFrame unwrapFrame = AcquireFrame(0);
                unwrapFrame.Function = frame.Function;
                unwrapFrame.BindingEnumerator = continuation.ToUnwrap;
                PushCallStack(unwrapFrame);
                return;
            }
            
            throw new NotImplementedException();
        }

        private void InterpretInstruction(Instruction ins)
        {
            switch (ins.Op) {
                case OpCode.NOOP:
                    InterpretNoOp(ins);
                    break;
                case OpCode.BREAK:
                    InterpretBreak(ins);
                    break;
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
                    InterpretReturn(ins);
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
                    InterpretBool(ins);
                    break;
                case OpCode.LNOT:
                    InterpretLogicalNot(ins);
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
        }

        private void InterpretNoOp(Instruction ins)
        {
        }

        private void InterpretBreak(Instruction ins)
        {
            --PeekCallStack().PC;
            Break();
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
            PushCall(currentFrame.Memory, ins.ImmediateValue);
        }

        private void InterpretReturn(Instruction ins)
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

        private void InterpretBool(Instruction ins)
        {
            _aRegister = _aRegister == 0 ? 0 : 1;
        }

        private void InterpretLogicalNot(Instruction ins)
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

        private void PushCall(StackFrameMemory argumentStackSource, int argumentStackStart)
        {
            // First value on the argument stack is the function index
            int functionIndex = argumentStackSource.Read(argumentStackStart);
            
            // The rest of the data on the argument stack is argument values
            int argumentValuesStart = argumentStackStart + 1;
            
            int argumentMemorySize;
            StackFrame frame;

            int definedFunctionCount = _module.Module.DefinedFunctions.Length;
            
            if (functionIndex >= definedFunctionCount) {
                VMFunction function = _module.VMFunctions[functionIndex];
                argumentMemorySize = function.ArgumentMemorySize;
                frame = AcquireFrame(function.ArgumentMemorySize);
                frame.BindingEnumerator = function.Delegate(this, new ArgumentSource(frame.Memory, 0));
            } else {
                ILFunction function = _module.Module.DefinedFunctions[functionIndex];
                argumentMemorySize = function.ArgumentMemorySize;
                frame = AcquireFrame(function.MaxStackSize);
            }

            frame.Function = functionIndex;
            frame.Memory.CopyFrom(argumentStackSource, argumentValuesStart, 0, argumentMemorySize);
            PushCallStack(frame);
        }

        private void PushCall(int functionIndex, IReadOnlyList<int> arguments)
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
            
            PushCall(_argumentBuffer, 0);
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
            StackFrame frame = PopCallStack();
            frame.BindingEnumerator = null;
            frame.PC = 0;
            _framePool.Push(frame);
        }
        
        private readonly Stack<StackFrame> _framePool = new Stack<StackFrame>();

        private void Abort()
        {
            _callStack.Clear();
            _isContinuing = false;
            _finishedCallback(false);
        }
    }
}