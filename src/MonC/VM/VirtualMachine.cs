using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MonC.Bytecode;
using MonC.Codegen;

namespace MonC.VM
{
    public class VirtualMachine
    {
        private readonly Stack<StackFrame> _callStack = new Stack<StackFrame>();
        private readonly List<int> _argumentStack = new List<int>();
        private ILModule _module;
        private int _aRegister;
        private int _bRegister;

        private bool _canContinue;

        public void LoadModule(ILModule module)
        {
            // TODO: Assert stopped.
            _module = module;
        }

        public void Call(string functionName, IEnumerable<int> arguments)
        {
            // TODO: Assert stopped.
            
            _argumentStack.AddRange(arguments);

            int functionIndex = LookupFunction(functionName);
            
            _callStack.Push(new StackFrame {
                Function = functionIndex
            });

            Continue();
        }

        private int LookupFunction(string functionName)
        {
            for (int i = 0, ilen = _module.ExportedFunctions.Length; i < ilen; ++i) {
                if (_module.ExportedFunctions[i] == functionName) {
                    return i;
                }
            }
            return -1;
        }

        private void Continue()
        {
            _canContinue = true;

            while (_canContinue) {
                InterpretCurrentInstruction();
            }
        }

        private void InterpretCurrentInstruction()
        {
            StackFrame top = _callStack.Peek();
            Instruction ins = _module.DefinedFunctions[top.Function][top.PC];
            ++top.PC;
            InterpretInstruction(ins);
        }

        private void InterpretInstruction(Instruction ins)
        {
            switch (ins.Op) {
                case OpCode.NOOP:
                    InterpretNoOp(ins);
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
                case OpCode.NOT:
                    InterpretNot(ins);
                    break;
                case OpCode.ADD:
                    InterpretAdd(ins);
                    break;
                case OpCode.ADDI:
                    InterpretAddI(ins);
                    break;
                case OpCode.SUB:
                    InterpretSub(ins);
                    break;
                case OpCode.SUBI:
                    InterpretSubI(ins);
                    break;
            }
        }

        private void InterpretNoOp(Instruction ins)
        {
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
            _aRegister = _callStack.Peek().Memory.Read(ins.ImmediateValue);
        }

        private void InterpretWrite(Instruction ins)
        {
            _callStack.Peek().Memory.Write(ins.ImmediateValue, _aRegister);
        }

        private void InterpretPushArg(Instruction ins)
        {
            _argumentStack.Add(_aRegister);
        }

        private void InterpretCall(Instruction ins)
        {
            StackFrame newFrame = new StackFrame { Function = ins.ImmediateValue };
            for (int i = 0, ilen = _argumentStack.Count; i < ilen; ++i) {
                newFrame.Memory.Write(i, _argumentStack[i]);
            }
            _argumentStack.Clear();
            _callStack.Push(newFrame);
        }

        private void InterpretReturn(Instruction ins)
        {
            // TODO: Handle end of code
            _callStack.Pop();
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

        private void InterpretNot(Instruction ins)
        {
            _aRegister = _aRegister == 0 ? 1 : 0;
        }

        private void InterpretAdd(Instruction ins)
        {
            _aRegister += _bRegister;
        }

        private void InterpretAddI(Instruction ins)
        {
            _aRegister += ins.ImmediateValue;
        }

        private void InterpretSub(Instruction ins)
        {
            _aRegister -= _bRegister;
        }

        private void InterpretSubI(Instruction ins)
        {
            _aRegister -= ins.ImmediateValue;
        }
        
        private void Jump(int offset)
        {
            _callStack.Peek().PC += offset;
        }
        
    }
}