using System;
using System.Collections.Generic;
using System.Linq;
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

        public event Action? Break;
        public event Action? ActiveChanged;

        public bool IsActive => _isActive;

        public Debugger(VMModule module, VirtualMachine vm)
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

            HandleActiveChanged();
        }

        public void SetBreakpoint(int function, int address)
        {
            Breakpoint breakpoint = new Breakpoint {Function = function, Address = address}; 
            _breakpoints.Add(breakpoint);
            ReplaceInstruction(breakpoint);
        }

        public void RemoveBreakpoint(int function, int address)
        {
            _breakpoints.Remove(new Breakpoint {Function = function, Address = address});
            RestoreInstruction(new StackFrameInfo {Function = function, PC = address});
            // TODO: Assert instruction restore was successful? 
        }

        public bool SetBreakpoint(string sourcePath, int lineNumber)
        {
            int func, addr;
            bool result = LookupSymbol(sourcePath, lineNumber, out func, out addr); 
            if (result) {
                SetBreakpoint(func, addr);
            }
            return result;
        }

        public bool RemoveBreakpoint(string sourcePath, int lineNumber)
        {
            int func, addr;
            bool result = LookupSymbol(sourcePath, lineNumber, out func, out addr);
            if (result) {
                RemoveBreakpoint(func, addr);
            }
            return result;
        }

        public bool LookupSymbol(string sourcePath, int lineNumber, out int functionIndexResult, out int addressResult)
        {
            // TODO: Caching might be necesary if this becomes too slow.
            for (int functionIndex = 0, funcLen = _module.Module.DefinedFunctions.Length; functionIndex < funcLen; ++functionIndex) {
                ILFunction function = _module.Module.DefinedFunctions[functionIndex];
                
                for (int i = 0, ilen = function.Code.Length; i < ilen; ++i) {
                    Symbol symbol;
                    if (!function.Symbols.TryGetValue(i, out symbol)) {
                        continue;
                    }
                    
                    if (symbol.SourceFile == null) {
                        continue;
                    }
                    
                    // TODO: More permissive path matching
                    if (symbol.SourceFile != sourcePath) {
                        // Assume that if the first symbol doesn't match the source path, none of them do.
                        break;
                    }

                    if (lineNumber >= symbol.Start.Line && lineNumber <= symbol.End.Line) {
                        functionIndexResult = functionIndex;
                        addressResult = i;
                        return true;
                    }
                }
            }

            functionIndexResult = 0;
            addressResult = 0;
            return false;
        }

        public bool GetSourceLocation(StackFrameInfo frame, out string? sourcePath, out int lineNumber)
        {
            sourcePath = "";
            lineNumber = 0;
            
            if (frame.Function < 0 || frame.Function >= _module.Module.DefinedFunctions.Length) {
                return false;
            }
            
            ILFunction function = _module.Module.DefinedFunctions[frame.Function];

            for (int i = frame.PC; i < function.Code.Length; ++i) {
                Symbol symbol;
                if (!function.Symbols.TryGetValue(i, out symbol)) {
                    continue;
                }

                sourcePath = symbol.SourceFile;
                lineNumber = (int)symbol.Start.Line;
                return true;
            }

            return false;
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

        public ILFunction GetILFunctionForFrame(StackFrameInfo frame)
        {
            if (frame.Function < 0 || frame.Function >= _module.Module.DefinedFunctions.Length) {
                return ILFunction.Empty();
            }
            return _module.Module.DefinedFunctions[frame.Function];
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
                HandleActiveChanged();
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
            
            HandleActiveChanged();
            
            _vm.Continue();
            
        }
        
        private void HandleBreak()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            if (RestoreInstruction(frame)) {

                bool wasActive = _isActive;
                
                _vm.SetStepping(true);
                _isActive = true;

                if (!wasActive) {
                    var handler = Break;
                    if (handler != null) {
                        handler();
                    }
                    
                    HandleActiveChanged();
                }
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
            if (frame.Function >= _module.Module.DefinedFunctions.Length) {
                return ILFunction.Empty();
            }
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

        private void HandleActiveChanged()
        {
            Action? handler = ActiveChanged;
            if (handler != null) {
                handler();
            }
        }
    }
}