using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.Codegen;
using MonC.VM;

namespace MonC.Debugging
{
    public class Debugger : IVMDebugger
    {
        private VirtualMachine _vm;
        private IDebuggableVM _debuggableVm;
        private VMModule _module;

        private readonly List<Dictionary<int, Instruction>> _replacedInstructions = new List<Dictionary<int, Instruction>>();
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        
        private bool _isActive;

        private bool _lastBreakWasCausedByBreakpoint;

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
            _debuggableVm = vm;
            _module = module;
            _vm.SetDebugger(this);
        }

        public void Pause()
        {
            _isActive = true;
            _debuggableVm.SetStepping(true);

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
        
        public ILFunction GetILFunction(StackFrameInfo frame)
        {
            if (frame.Function < 0 || frame.Function >= _module.Module.DefinedFunctions.Length) {
                return ILFunction.Empty();
            }
            return _module.Module.DefinedFunctions[frame.Function];
        }

        public void StepInto()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot StepInto while debugger is inactive");
            }
            
            // Step once and restore breakpoints 
            StepInternal();
            
            // Keep stepping until we find a debug symbol.
            while (_vm.IsRunning) {
                StackFrameInfo frame = _vm.GetStackFrame(0);
                if (GetSymbol(frame, out _)) {
                    break;
                }
                _debuggableVm.Continue();

                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step into
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
            }
        }

        public void StepOver()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot StepOver while debugger is inactive");
            }
            
            StackFrameInfo frame = _vm.GetStackFrame(0);

            // Where in the file were we?
            string? sourcePath;
            int lineNumber;
            GetSourceLocation(frame, out sourcePath, out lineNumber);

            if (sourcePath == null) {
                // There's no symbols for the code we're in. Just do a single step.
                _debuggableVm.Continue();
                return;
            }
            
            // Find a breakpoint for a line past the current line number.
            Breakpoint breakpoint;
            if (!FindNextBreakpointPastLineNumber(frame, sourcePath, lineNumber, out breakpoint)) {
                // No more symbols past the given line? Just step out.
                StepOutInternal();
                return;
            }
            
            // Apply the transient breakpoint for the next line.
            ReplaceInstruction(breakpoint);
            
            // Step and re-apply breakpoints
            StepInternal();

            if (_lastBreakWasCausedByBreakpoint) {
                // Breakpoint encountered while restoring breakpoints, either for our transient breakpoint or some other one.
                // Try to remove the transient breakpoint for the later case.
                RestoreInstruction(breakpoint);
                return;
            }
            
            ContinueInternal();
        }

        public void StepOut()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot StepOut while debugger is inactive");
            }

            StepOutInternal();
        }

        private void StepOutInternal()
        {
            int stackFrameCount = _vm.CallStackFrameCount;
            StackFrameInfo previousFrame = _vm.GetStackFrame(1);

            Breakpoint breakpoint = new Breakpoint {Address = previousFrame.PC, Function = previousFrame.Function};
            if (stackFrameCount > 1) {
                // Apply a non-persistent breakpoint to the location where the current frame will return.
                // The PC of the previous frame will always be the point where execution will resume when the current
                // frame is popped.
                ReplaceInstruction(breakpoint);
            }
            
            // Step once and re-apply breakpoints
            StepInternal();

            if (_lastBreakWasCausedByBreakpoint) {
                // When we continued, a breakpoint was hit. That was either our transient breakpoint we just set or
                // some other breakpoint. No matter the case, we shouldn't continue stepping out 
                // (unless in the future we imlement a 'force' step out, like IntelliJ has)

                // Try removing the transient breakpoint we just set incase it wasn't hit, so it isn't hit later.
                if (stackFrameCount > 1) {
                    RestoreInstruction(breakpoint);
                }
                return;
            }
            
            ContinueInternal();
        }

        public void Continue()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot Continue while debugger is inactive");
            }

            // Step once and re-apply breakpoints
            StepInternal();

            if (_lastBreakWasCausedByBreakpoint) {
                // Hit another breakpoint. Don't continue.
                // (In there future there may be force functionality that ignores this and continues anyway.)
                return;
            }
            
            ContinueInternal();
        }

        private void ContinueInternal()
        {
            _isActive = false;
            _debuggableVm.SetStepping(false);
            _debuggableVm.Continue();
            HandleActiveChanged();
        }

        public void Step()
        {
            if (!_isActive) {
                throw new InvalidOperationException("Cannot Step while debugger is inactive");
            }
            
            StepInternal();
        }

        private void StepInternal()
        {
            // Step once and re-apply breakpoints
            _debuggableVm.Continue();
            ApplyBreakpoints();
        }
        
        void IVMDebugger.HandleBreak()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            _lastBreakWasCausedByBreakpoint = RestoreInstruction(frame);;
            if (_lastBreakWasCausedByBreakpoint) {

                bool wasActive = _isActive;
                
                _debuggableVm.SetStepping(true);
                _isActive = true;

                if (!wasActive) {
                    var handler = Break;
                    if (handler != null) {
                        handler();
                    }
                    
                    HandleActiveChanged();
                }
            }
            
        }

        private bool FindBreakpointForNextSymbol(StackFrameInfo frame, out Breakpoint breakpoint)
        {
            ILFunction func = GetILFunction(frame);
            
            int minAddress = func.Code.Length - 1;
            bool foundBreakpoint = false;

            foreach (int symbolAddress in func.Symbols.Keys) {
                if (symbolAddress > frame.PC && symbolAddress < minAddress) {
                    minAddress = symbolAddress;
                    foundBreakpoint = true;
                }
            }

            breakpoint = new Breakpoint {Function = frame.Function, Address = minAddress};
            return foundBreakpoint;
        }

        private bool GetSymbol(StackFrameInfo frame, out Symbol symbol)
        {
            ILFunction func = GetILFunction(frame);
            return func.Symbols.TryGetValue(frame.PC, out symbol);
        }

        private bool FindNextBreakpointPastLineNumber(StackFrameInfo frame, string sourcePath, int lineNumber, out Breakpoint breakpoint)
        {
            ILFunction function = GetILFunction(frame);
            foreach (KeyValuePair<int, Symbol> addressAndSymbol in function.Symbols) {
                Symbol symbol = addressAndSymbol.Value;
                if (symbol.SourceFile == sourcePath && symbol.Start.Line > lineNumber) {
                    breakpoint = new Breakpoint{ Function = frame.Function, Address = addressAndSymbol.Key};
                    return true;
                }
            }

            breakpoint = default;
            return false;
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
            return RestoreInstruction(new Breakpoint {Address = frame.PC, Function = frame.Function});
        }

        private bool RestoreInstruction(Breakpoint breakpoint)
        {
            if (breakpoint.Function < 0 || breakpoint.Function >= _replacedInstructions.Count) {
                return false;
            }
            
            Instruction ins;
            
            var instructions = _replacedInstructions[breakpoint.Function];
            if (!instructions.TryGetValue(breakpoint.Address, out ins)) {
                return false;
            }

            instructions.Remove(breakpoint.Address);
            
            _module.Module.DefinedFunctions[breakpoint.Function].Code[breakpoint.Address] = ins;
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