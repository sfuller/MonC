using System;
using System.Collections.Generic;
using MonC.Bytecode;

namespace MonC.VM
{
    public class VirtualMachine : IVMBindingContext
    {
        private readonly List<StackFrame> _callStack = new List<StackFrame>();
        private readonly List<int> _argumentStack = new List<int>();
        private VMModule _module;
        private int _aRegister;
        private int _bRegister;
        
        private bool _canContinue;

        private Action _breakHandler;
        private bool _isStepping;

        public bool IsRunning => _callStack.Count != 0;

        public void LoadModule(VMModule module)
        {
            if (IsRunning) {
                throw new InvalidOperationException("Cannot load module while running");
            }
            _module = module;
        }

        public void Call(string functionName, IEnumerable<int> arguments, bool start = true)
        {
            if (IsRunning) {
                throw new InvalidOperationException("Cannot call function while running");
            }
            
            int functionIndex = LookupFunction(functionName);

            if (functionIndex == -1) {
                throw new ArgumentException(
                    message:   "No function by the given name was found in the loaded module",
                    paramName: nameof(functionName));
            }
            
            _argumentStack.AddRange(arguments);
            
            PushCall(functionIndex);

            if (start) {
                Continue();    
            }
        }

        public void SetBreakHandler(Action handler)
        {
            _breakHandler = handler;
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
            if (_canContinue) {
                return;
            }
            
            _canContinue = true;
            
            while (_canContinue) {
                InterpretCurrentInstruction();
            }
        }

        public void SetStepping(bool isStepping)
        {
            _isStepping = isStepping;
        }

        public StackFrameInfo GetStackFrame(int depth)
        {
            if (depth >= _callStack.Count) {
                return new StackFrameInfo();
            }

            StackFrame internalFrame = _callStack[_callStack.Count - 1 - depth];
            return new StackFrameInfo {
                Function = internalFrame.Function,
                PC = internalFrame.PC
            };
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

        private void PushCallStack(StackFrame frame)
        {
            _callStack.Add(frame);
        }

        private void Break()
        {
            _canContinue = false;
            if (_breakHandler != null) {
                _breakHandler();
            }
        }

        private void InterpretCurrentInstruction()
        {
            if (_callStack.Count == 0) {
                _canContinue = false;
                return;
            }

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
            if (!frame.BindingEnumerator.MoveNext()) {
                // Function has finished
                PopFrame();
                return;
            }

            Continuation continuation = frame.BindingEnumerator.Current;

            if (continuation.Action == ContinuationAction.CALL) {
                _argumentStack.AddRange(continuation.Arguments);
                PushCall(continuation.FunctionIndex);
                return;
            }

            if (continuation.Action == ContinuationAction.RETURN) {
                _aRegister = continuation.ReturnValue;
                PopFrame();
                return;
            }

            if (continuation.Action == ContinuationAction.YIELD) {
                _canContinue = false;
                continuation.YieldToken.OnFinished(Continue);

                // Remember: Caling Start can call the finish callback, so call Start at the very end.
                continuation.YieldToken.Start();
                
                return;
            }

            if (continuation.Action == ContinuationAction.UNWRAP) {
                StackFrame unwrapFrame = AcquireFrame();
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
                case OpCode.LOADB:
                    InterpretLoadB(ins);
                    break;
                case OpCode.READ:
                    InterpretRead(ins);
                    break;
                case OpCode.WRITE:
                    InterpretWrite(ins);
                    break;
                case OpCode.PUSHARG:
                    InterpretPushArg(ins);
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
                case OpCode.NOT:
                    InterpretNot(ins);
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

        private void InterpretLoadB(Instruction ins)
        {
            _bRegister = _aRegister;
        }

        private void InterpretRead(Instruction ins)
        {
            _aRegister = PeekCallStack().Memory.Read(ins.ImmediateValue);
        }

        private void InterpretWrite(Instruction ins)
        {
            PeekCallStack().Memory.Write(ins.ImmediateValue, _aRegister);
        }

        private void InterpretPushArg(Instruction ins)
        {
            _argumentStack.Add(_aRegister);
        }

        private void InterpretCall(Instruction ins)
        {
            PushCall(ins.ImmediateValue);
        }

        private void InterpretReturn(Instruction ins)
        {
            PopFrame();
        }

        private void InterpretCmpE(Instruction ins)
        {
            _aRegister = _aRegister == _bRegister ? 1 : 0;
        }

        private void InterpretCmpLT(Instruction ins)
        {
            _aRegister = _aRegister < _bRegister ? 1 : 0;
        }

        private void InterpretCmpLTE(Instruction ins)
        {
            _aRegister = _aRegister <= _bRegister ? 1 : 0;
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

        private void InterpretNot(Instruction ins)
        {
            _aRegister = ~_aRegister;
        }

        private void InterpretAdd(Instruction ins)
        {
            _aRegister += _bRegister;
        }
        
        private void InterpretSub(Instruction ins)
        {
            _aRegister -= _bRegister;
        }

        private void InterpretAnd(Instruction ins)
        {
            _aRegister &= _bRegister;
        }

        private void InterpretOr(Instruction ins)
        {
            _aRegister |= _bRegister;
        }

        private void InterpretMul(Instruction ins)
        {
            _aRegister *= _bRegister;
        }

        private void InterpretDiv(Instruction ins)
        {
            _aRegister /= _bRegister;
        }

        private void InterpretMod(Instruction ins)
        {
            _aRegister %= _bRegister;
        }
        
        private void Jump(int offset)
        {
            PeekCallStack().PC += offset;
        }

        private void PushCall(int functionIndex)
        {
            StackFrame newFrame = AcquireFrame();
            newFrame.Function = functionIndex;

            if (functionIndex >= _module.Module.DefinedFunctions.Length) {
                VMEnumerable enumerable = _module.VMFunctions[functionIndex];
                int[] args = _argumentStack.ToArray();
                _argumentStack.Clear();
                newFrame.BindingEnumerator = enumerable(this, args);
                PushCallStack(newFrame);
            } else {
                for (int i = 0, ilen = _argumentStack.Count; i < ilen; ++i) {
                    newFrame.Memory.Write(i, _argumentStack[i]);
                }
                _argumentStack.Clear();
                PushCallStack(newFrame);
            }
        }

        int IVMBindingContext.ReturnValue => _aRegister;

        private StackFrame AcquireFrame()
        {
            if (_framePool.Count > 0) {
                return _framePool.Pop();
            }
            return new StackFrame();
        }

        private void PopFrame()
        {
            StackFrame frame = PopCallStack();
            frame.BindingEnumerator = null;
            frame.PC = 0;
            _framePool.Push(frame);
        }

        private readonly Stack<StackFrame> _framePool = new Stack<StackFrame>();
    }
}