using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.Codegen;
using MonC.SyntaxTree;
using MonC.VM;

namespace MonC.Debugging
{
    public class Debugger
    {
        private VirtualMachine _vm;
        private VMModule _module;

        private readonly List<Dictionary<int, Instruction>> _replacedInstructions = new List<Dictionary<int, Instruction>>();
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        
        private bool _isActive;
        
        public void Setup(VMModule module, VirtualMachine vm)
        {
            _replacedInstructions.Clear();
            for (int i = 0, ilen = module.Module.DefinedFunctions.Length; i < ilen; ++i) {
                _replacedInstructions.Add(new Dictionary<int, Instruction>());
            }
            
            _vm = vm;
            _module = module;
            _vm.SetBreakHandler(HandleBreak);
        }

        public void Pause()
        {
            _isActive = true;
            _vm.SetStepping(true);
        }

        public void SetBreakpoint(int function, int address)
        {
            _breakpoints.Add(new Breakpoint {Function = function, Address = address});
        }

        public void SetBreakpoint(string sourcePath, int lineNumber)
        {
            // TODO: Lookup address
            throw new NotImplementedException();
        }

        public void StepInto()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot StepInto while debugger is inactive");
            }

            // Step and re-apply breakpoints
            _vm.Continue();
            ApplyBreakpoints();

            while (!CanFinishStepping()) {
                _vm.Continue();
            }
        }

        public void StepNext()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot StepNext while debugger is inactive");
            }
            
            StackFrameInfo frame = _vm.GetStackFrame(0);
            ILFunction func = GetILFunction(frame);
            Instruction instruction = func.Code[frame.PC];
            
            // Step once and re-apply breakpoints
            _vm.Continue();
            ApplyBreakpoints();
            
            if (instruction.Op == OpCode.CALL) {
                Breakpoint bp = FindBreakpointForNextSymbol(frame);
                ReplaceInstruction(bp);
                _isActive = false;
                _vm.SetStepping(false);
                _vm.Continue();
            } else {
                while (!CanFinishStepping()) {
                    _vm.Continue();
                }
            }
        }

        public void Continue()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot Continue while debugger is inactive");
            }

            // Step once and re-apply breakpoints
            _vm.Continue();
            ApplyBreakpoints();
            
            _isActive = false;
            _vm.SetStepping(false);
            _vm.Continue();
        }
        
        private void HandleBreak()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            if (RestoreInstruction(frame)) {
                _vm.SetStepping(true);
                _isActive = true;    
            }
            
//            // Is there a breakpoint set for this address?
//            if (_breakpoints.Contains(new Breakpoint {Function = frame.Function, Address = frame.PC})) {
//                _vm.SetStepping(true);
//                _isActive = true;
//            }

//            // Do we have symbols for the current execution address?
//            ILFunction func = GetILFunction(frame);
//            if (func.Symbols.ContainsKey(frame.PC)) {
//                _vm.SetStepping(true);
//                _isActive = true;
//            }
        }

        private bool CanFinishStepping() 
        {
            if (!_vm.IsRunning) {
                return true;
            }
            
            StackFrameInfo frame = _vm.GetStackFrame(0);
            
            // Is there a breakpoint set for this address?
            if (_breakpoints.Contains(new Breakpoint {Function = frame.Function, Address = frame.PC})) {
                return true;
            }
            
            // Do we have symbols for the current execution address?
            ILFunction func = GetILFunction(frame);
            return func.Symbols.ContainsKey(frame.PC);
        }

        private Breakpoint FindBreakpointForNextSymbol(StackFrameInfo frame)
        {
            ILFunction func = GetILFunction(frame);
            
            int minAddress = func.Code.Length - 1;

            foreach (int symbolAddress in func.Symbols.Keys) {
                if (symbolAddress > frame.PC && symbolAddress < minAddress) {
                    minAddress = symbolAddress;
                }
            }

            return new Breakpoint {Function = frame.Function, Address = minAddress};
        }
        
        private ILFunction GetILFunction(StackFrameInfo frame)
        {
            return _module.Module.DefinedFunctions[frame.Function];
        }

        private void ReplaceInstruction(Breakpoint breakpoint)
        {
            if (breakpoint.Function < 0 || breakpoint.Function >= _replacedInstructions.Count) {
                // TODO: Log something
                return;
            }

            var instructions = _replacedInstructions[breakpoint.Function];
            if (instructions.ContainsKey(breakpoint.Address)) {
                return;
            }
            
            ILFunction[] moduleFunctions = _module.Module.DefinedFunctions;
            if (breakpoint.Function >= moduleFunctions.Length) {
                // TODO: Log something
                return;
            }

            ILFunction function = moduleFunctions[breakpoint.Function];

            if (breakpoint.Address < 0 || breakpoint.Address >= function.Code.Length) {
                // TODO: Log something
                return;
            }

            Instruction ins = function.Code[breakpoint.Address];
            instructions.Add(breakpoint.Address, ins);
            function.Code[breakpoint.Address] = new Instruction(OpCode.BREAK);
        }
        
        private bool RestoreInstruction(StackFrameInfo frame)
        {
            if (frame.Function < 0 || frame.Function >= _replacedInstructions.Count) {
                return false;
            }

            Instruction ins;
            
            var instructions = _replacedInstructions[frame.Function];
            if (!instructions.TryGetValue(frame.PC, out ins)) {
                return false;
            }

            instructions.Remove(frame.PC);
            
            _module.Module.DefinedFunctions[frame.Function].Code[frame.PC] = ins;
            return true;
        }

        private void ApplyBreakpoints()
        {
            foreach (Breakpoint breakpoint in _breakpoints) {
                ReplaceInstruction(breakpoint);
            }
        }
        
    }
}