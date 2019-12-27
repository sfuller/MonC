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
            STEP,
            CONTINUE
        }

        private struct ModuleDebugData
        {
            public Dictionary<int, Dictionary<int, Instruction>> ReplacedInstructionsByFunction;
        }
        
        private readonly VirtualMachine _vm;
        private readonly IDebuggableVM _debuggableVm;

        private readonly Dictionary<VMModule, ModuleDebugData> _debugDataByModule = new Dictionary<VMModule, ModuleDebugData>();
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        
        private bool _lastBreakWasCausedByBreakpoint;

        private IEnumerator<ActionRequest>? _currentAction;

        public event Action? Break;
        public event Action? PauseChanged;

        public Debugger(VirtualMachine vm)
        {
            _vm = vm;
            _debuggableVm = vm;
            _vm.SetDebugger(this);
        }

        public void Pause()
        {
            _debuggableVm.SetStepping(true);
        }

        public void SetBreakpoint(string sourcePath, int lineNumber)
        {
            Breakpoint breakpoint = new Breakpoint {SourcePath = sourcePath, LineNumber = lineNumber};
            _breakpoints.Add(breakpoint);
            ReplaceInstructionForBreakpoint(breakpoint);
        }

        public void RemoveBreakpoint(string sourcePath, int lineNumber)
        {
            Breakpoint breakpoint = new Breakpoint {SourcePath = sourcePath, LineNumber = lineNumber};
            RestoreInstructionForBreakpoint(breakpoint);
        }

        public bool LookupSymbol(VMModule module, string sourcePath, int lineNumber, out int functionIndexResult, out int addressResult)
        {
            for (int functionIndex = 0, funcLen = module.ILModule.DefinedFunctions.Length; functionIndex < funcLen; ++functionIndex) {
                ILFunction function = module.ILModule.DefinedFunctions[functionIndex];
                
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
            
            if (frame.Function < 0 || frame.Function >= frame.Module.ILModule.DefinedFunctions.Length) {
                return false;
            }
            
            ILFunction function = frame.Module.ILModule.DefinedFunctions[frame.Function];

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
            if (frame.Function < 0 || frame.Function >= frame.Module.ILModule.DefinedFunctions.Length) {
                return ILFunction.Empty();
            }
            return frame.Module.ILModule.DefinedFunctions[frame.Function];
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
            // Keep stepping until we find a debug symbol.
            while (_vm.IsRunning) {
                yield return ActionRequest.STEP;
                
                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step into
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
                
                StackFrameInfo frame = _vm.GetStackFrame(0);
                if (GetSymbol(frame, out _)) {
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
            
            // Keep stepping until we find a debug symbol in the same function
            while (_vm.IsRunning) {
                yield return ActionRequest.STEP;
                
                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step over
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
                
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
            
            if (stackFrameCount > 1) {
                // Apply a non-persistent breakpoint to the location where the current frame will return.
                // The PC of the previous frame will always be the point where execution will resume when the current
                // frame is popped.
                ReplaceInstruction(previousFrame.Module, previousFrame.Function, previousFrame.PC);
            }
            
            yield return ActionRequest.CONTINUE;
            
            if (_lastBreakWasCausedByBreakpoint) {
                // When we continued, a breakpoint was hit. That was either our transient breakpoint we just set or
                // some other breakpoint. No matter the case, we shouldn't continue stepping out 
                // (unless in the future we imlement a 'force' step out, like IntelliJ has)

                // Try removing the transient breakpoint we just set incase it wasn't hit, so it isn't hit later.
                if (stackFrameCount > 1) {
                    RestoreInstruction(previousFrame.Module, previousFrame.Function, previousFrame.PC);
                }
            }
            
        }

        public bool Continue()
        {
            return StartAction(ContinueAction());
        }
        
        private IEnumerator<ActionRequest> ContinueAction()
        {
            yield return ActionRequest.CONTINUE;
        }

        public bool Step()
        {
            return StartAction(StepAction());
        }

        private IEnumerator<ActionRequest> StepAction()
        {
            yield return ActionRequest.STEP;
        }
        
        void IVMDebugger.HandleBreak()
        {
            StackFrameInfo frame = _vm.GetStackFrame(0);
            _lastBreakWasCausedByBreakpoint = RestoreInstruction(frame.Module, frame.Function, frame.PC);
            if (_lastBreakWasCausedByBreakpoint) {

                _debuggableVm.SetStepping(true);
            }

            while (UpdateAction(isStartingAction: false)) { }
        }
        
        private bool _isUpdating;
        private bool _didBreakImmediatley;
        private IEnumerator<object?>? _updateRequest;

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

            if (_updateRequest != null) {
                if (!_updateRequest.MoveNext()) {
                    _updateRequest.Dispose();
                    _updateRequest = null;
                } else {
                    return false;
                }
            }

            if (_currentAction != null && !_currentAction.MoveNext()) {
                _currentAction.Dispose();
                _currentAction = null;
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
            
            _updateRequest = RequestRoutine(_currentAction.Current);
            return true;
        }

        private IEnumerator<object?> RequestRoutine(ActionRequest request)
        {
            if (_lastBreakWasCausedByBreakpoint || request == ActionRequest.STEP) {
                _debuggableVm.SetStepping(true);
                StackFrameInfo originFrame = _vm.GetStackFrame(0);
                if (!ContinueVM()) {
                    yield return null;
                }
                
                // Restore the breakpoint for the frame we are stepping from, but don't apply breakpoints for other 
                // instructions as we might have just broken on one of those and need the instruction to be restored
                // in order to continue.
                ReApplyBreakpoint(originFrame);
            }
            
            if (request == ActionRequest.CONTINUE) {
                _debuggableVm.SetStepping(false);
                if (!ContinueVM()) {
                    yield return null;
                }
            }
        }

        private bool ContinueVM()
        {
            _isUpdating = true;
            _didBreakImmediatley = false;
            _debuggableVm.Continue();
            _isUpdating = false;
            return _didBreakImmediatley;
        }

        void IVMDebugger.HandleFinished()
        {
            // TODO: Need to restore all replaced instructions!
            // TODO: This only needs to be done if the module can be re-used by other debuggers.

            HandlePausedChanged();
        }

        void IVMDebugger.HandleModuleAdded(VMModule module)
        {
            if (_debugDataByModule.ContainsKey(module)) {
                return;
            }

            ModuleDebugData data = new ModuleDebugData {
                ReplacedInstructionsByFunction = new Dictionary<int, Dictionary<int, Instruction>>()
            };
            _debugDataByModule.Add(module, data);
            
            // TODO: PERF: Only apply breakpoints for given module, not accross all loaded modules.
            ApplyBreakpoints();
        }

        private bool GetSymbol(StackFrameInfo frame, out Symbol symbol)
        {
            ILFunction func = GetILFunction(frame);
            return func.Symbols.TryGetValue(frame.PC, out symbol);
        }

        private void ReplaceInstructionForBreakpoint(Breakpoint breakpoint)
        {
            foreach (VMModule module in _debugDataByModule.Keys) {
                ReplaceInstructionForBreakpointInModule(breakpoint, module);
            }
        }

        private void ReplaceInstructionForBreakpointInModule(Breakpoint breakpoint, VMModule module)
        {
            int functionIndex, address;
            if (!LookupSymbol(module, breakpoint.SourcePath, breakpoint.LineNumber, out functionIndex, out address)) {
                return;
            }

            ReplaceInstruction(module, functionIndex, address);
        }

        private void ReplaceInstruction(VMModule module, int functionIndex, int address)
        {
            ModuleDebugData debugData;
            if (!_debugDataByModule.TryGetValue(module, out debugData)) {
                return;
            }
            
            Dictionary<int, Instruction> replacedInstructions;
            if (!debugData.ReplacedInstructionsByFunction.TryGetValue(functionIndex, out replacedInstructions)) {
                replacedInstructions = new Dictionary<int, Instruction>();
                debugData.ReplacedInstructionsByFunction[functionIndex] = replacedInstructions;
            }

            if (replacedInstructions.ContainsKey(address)) {
                // Instruction already replaced
                return;
            }
            
            ILFunction function = module.ILModule.DefinedFunctions[functionIndex];
            Instruction ins = function.Code[address];
            replacedInstructions.Add(address, ins);
            function.Code[address] = new Instruction(OpCode.BREAK);
        }

        private void RestoreInstructionForBreakpoint(Breakpoint breakpoint)
        {
            foreach (VMModule module in _debugDataByModule.Keys) {
                int function, address;
                if (LookupSymbol(module, breakpoint.SourcePath, breakpoint.LineNumber, out function, out address)) {
                    RestoreInstruction(module, function, address);
                }
            }
        }
        
        private bool RestoreInstruction(VMModule module, int functionIndex, int address)
        {
            ModuleDebugData debugData;
            if (!_debugDataByModule.TryGetValue(module, out debugData)) {
                return false;
            }

            Dictionary<int, Instruction> replacedInstructions;
            if (!debugData.ReplacedInstructionsByFunction.TryGetValue(functionIndex, out replacedInstructions)) {
                return false;
            }

            Instruction originalInstruction;
            if (!replacedInstructions.TryGetValue(address, out originalInstruction)) {
                return false;
            }

            replacedInstructions.Remove(address);
            module.ILModule.DefinedFunctions[functionIndex].Code[address] = originalInstruction;
            return true;
        }

        private void ApplyBreakpoints()
        {
            foreach (Breakpoint breakpoint in _breakpoints) {
                ReplaceInstructionForBreakpoint(breakpoint);
            }
        }

        private void ReApplyBreakpoint(StackFrameInfo info)
        {
            foreach (Breakpoint breakpoint in _breakpoints) {
                int functionIndex, address;
                if (!LookupSymbol(info.Module, breakpoint.SourcePath, breakpoint.LineNumber, out functionIndex, out address)) {
                    continue;
                }

                if (functionIndex == info.Function && address == info.PC) {
                    ReplaceInstructionForBreakpoint(breakpoint);    
                }
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