using System;
using System.Collections.Generic;
using MonC.Bytecode;
using MonC.Codegen;
using MonC.VM;

namespace MonC.Debugging
{
    public class Debugger : IVMDebugger
    {
        private enum ActionRequest
        {
            UNKNOWN,
            STEP_AND_APPLY_BREAKPOINTS,
            STEP,
            CONTINUE
        }
        
        private readonly VirtualMachine _vm;
        private readonly IDebuggableVM _debuggableVm;
        private readonly VMModule _module;

        private readonly List<Dictionary<int, Instruction>> _replacedInstructions = new List<Dictionary<int, Instruction>>();
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        
        private bool _lastBreakWasCausedByBreakpoint;

        private IEnumerator<ActionRequest>? _currentAction;

        public event Action? Break;
        public event Action? PauseChanged;

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
            _debuggableVm.SetStepping(true);
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

        /// <summary>
        /// Start an action.
        /// Returns true if the debugger breaks while starting the action. The break handler will not be called in this
        /// case, so the return value of this method should be observed to handle the break in this case.
        /// </summary>
        private bool StartAction(IEnumerator<ActionRequest> action)
        {
            if (!_debuggableVm.IsPaused) {
                throw new InvalidOperationException();
            }
            
            if (_currentAction != null) {
                throw new InvalidOperationException();
            }
            
            _currentAction = action;
            
            while (UpdateAction(isStartingAction: true)) { }

            return _currentAction == null;
        }
        
        public bool StepInto()
        {
            return StartAction(StepIntoAction());
        }
        
        private IEnumerator<ActionRequest> StepIntoAction()
        {
            yield return ActionRequest.STEP_AND_APPLY_BREAKPOINTS;

            // Keep stepping until we find a debug symbol.
            while (_vm.IsRunning) {
                StackFrameInfo frame = _vm.GetStackFrame(0);
                if (GetSymbol(frame, out _)) {
                    break;
                }

                yield return ActionRequest.STEP;
                //_debuggableVm.Continue();

                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step into
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
            }
        }

        public bool StepOver()
        {
            return StartAction(StepOverAction());
        }
        
        private IEnumerator<ActionRequest> StepOverAction()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            int currentFunction = frame.Function;
            int minDepth = _vm.CallStackFrameCount;
            
            yield return ActionRequest.STEP_AND_APPLY_BREAKPOINTS;

            // Keep stepping until we find a debug symbol in the same function
            while (_vm.IsRunning) {
                frame = _vm.GetStackFrame(0);

                if (frame.Function == currentFunction) {
                    if (GetSymbol(frame, out _)) {
                        break;
                    }    
                }

                if (_vm.CallStackFrameCount < minDepth) {
                    // The frame we started stepping in has returned. Stop stepping.
                    break;
                }

                yield return ActionRequest.STEP;

                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step over
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
            }
            
        }

        public bool StepOut()
        {
            return StartAction(StepOutAction());
        }
        
        private IEnumerator<ActionRequest> StepOutAction()
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
            
            yield return ActionRequest.STEP_AND_APPLY_BREAKPOINTS;

            if (_lastBreakWasCausedByBreakpoint) {
                // When we continued, a breakpoint was hit. That was either our transient breakpoint we just set or
                // some other breakpoint. No matter the case, we shouldn't continue stepping out 
                // (unless in the future we imlement a 'force' step out, like IntelliJ has)

                // Try removing the transient breakpoint we just set incase it wasn't hit, so it isn't hit later.
                if (stackFrameCount > 1) {
                    RestoreInstruction(breakpoint);
                }
                yield break;
            }

            yield return ActionRequest.CONTINUE;
        }

        public bool Continue()
        {
            return StartAction(ContinueAction());
        }
        
        private IEnumerator<ActionRequest> ContinueAction()
        {
            // Step once and re-apply breakpoints
            yield return ActionRequest.STEP_AND_APPLY_BREAKPOINTS;

            if (_lastBreakWasCausedByBreakpoint) {
                // Hit another breakpoint. Don't continue.
                // (In there future there may be force functionality that ignores this and continues anyway.)
                yield break;
            }

            yield return ActionRequest.CONTINUE;
        }

        public bool Step()
        {
            return StartAction(StepAction());
        }

        private IEnumerator<ActionRequest> StepAction()
        {
            yield return ActionRequest.STEP_AND_APPLY_BREAKPOINTS;
        }
        
        void IVMDebugger.HandleBreak()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            _lastBreakWasCausedByBreakpoint = RestoreInstruction(frame);
            if (_lastBreakWasCausedByBreakpoint) {

                _debuggableVm.SetStepping(true);
            }

            while (UpdateAction(isStartingAction: false)) { }
        }

        private ActionRequest _lastRequest;

        
        private bool _isUpdating;
        private bool _didBreakImmediatley;
        
        /// <summary>
        /// Update the current action.
        ///
        /// </summary>
        private bool UpdateAction(bool isStartingAction)
        {
            if (_isUpdating) {
                _didBreakImmediatley = true;
                return false;
            }

            switch (_lastRequest) {
                case ActionRequest.STEP_AND_APPLY_BREAKPOINTS:
                    ApplyBreakpoints();
                    break;
                case ActionRequest.CONTINUE:
                    HandlePausedChanged();
                    break;
            }
            
            if (_currentAction != null && !_currentAction.MoveNext()) {
                _currentAction.Dispose();
                _currentAction = null;
                _lastRequest = ActionRequest.UNKNOWN;
            }
            
            if (_currentAction == null) {
                if (!isStartingAction) {
                    var handler = Break;
                    if (handler != null) {
                        handler();
                    }    
                }
                return false;
            }

            ActionRequest request = _currentAction.Current;
            _lastRequest = request;


            switch (request) {
                case ActionRequest.STEP:
                case ActionRequest.STEP_AND_APPLY_BREAKPOINTS:
                    _debuggableVm.SetStepping(true);
                    break;
                case ActionRequest.CONTINUE:
                    _debuggableVm.SetStepping(false);
                    break;
            }

            _isUpdating = true;
            _didBreakImmediatley = false;
            _debuggableVm.Continue();
            _isUpdating = false;
            
            if (request == ActionRequest.CONTINUE) {
                HandlePausedChanged();
            }
            
            return _didBreakImmediatley;
        }

        void IVMDebugger.HandleFinished()
        {
            // TODO: Need to restore all replaced instructions!
            
            HandlePausedChanged();
        }

//        private bool FindBreakpointForNextSymbol(StackFrameInfo frame, out Breakpoint breakpoint)
//        {
//            ILFunction func = GetILFunction(frame);
//            
//            int minAddress = func.Code.Length - 1;
//            bool foundBreakpoint = false;
//
//            foreach (int symbolAddress in func.Symbols.Keys) {
//                if (symbolAddress > frame.PC && symbolAddress < minAddress) {
//                    minAddress = symbolAddress;
//                    foundBreakpoint = true;
//                }
//            }
//
//            breakpoint = new Breakpoint {Function = frame.Function, Address = minAddress};
//            return foundBreakpoint;
//        }

        private bool GetSymbol(StackFrameInfo frame, out Symbol symbol)
        {
            ILFunction func = GetILFunction(frame);
            return func.Symbols.TryGetValue(frame.PC, out symbol);
        }

//        private bool FindNextBreakpointPastLineNumber(StackFrameInfo frame, string sourcePath, int lineNumber, out Breakpoint breakpoint)
//        {
//            ILFunction function = GetILFunction(frame);
//            foreach (KeyValuePair<int, Symbol> addressAndSymbol in function.Symbols) {
//                Symbol symbol = addressAndSymbol.Value;
//                if (symbol.SourceFile == sourcePath && symbol.Start.Line > lineNumber) {
//                    breakpoint = new Breakpoint{ Function = frame.Function, Address = addressAndSymbol.Key};
//                    return true;
//                }
//            }
//
//            breakpoint = default;
//            return false;
//        }

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

        private void HandlePausedChanged()
        {
            Action? handler = PauseChanged;
            if (handler != null) {
                handler();
            }
        }
    }
}