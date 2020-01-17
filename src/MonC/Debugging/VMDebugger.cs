using System;
using System.Collections.Generic;
using MonC.VM;

namespace MonC.Debugging
{
    public class VMDebugger : IVMDebugger
    {
        private enum ActionRequest
        {
            STEP,
            CONTINUE
        }

        private readonly Debugger _debugger;
        private readonly VirtualMachine _vm;
        private readonly IDebuggableVM _debuggableVm;
        
        private IEnumerator<ActionRequest>? _currentAction;
        
        private bool _lastBreakWasCausedByBreakpoint;
        private bool _isPaused;
        
        public event Action? Break;
        public event Action? PauseChanged;

        public VMDebugger(Debugger debugger, VirtualMachine vm)
        {
            _debugger = debugger;
            _vm = vm;
            _debuggableVm = vm;
            _vm.SetDebugger(this);
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
            StackFrameInfo frame = _vm.GetStackFrame(0);
            int currentFunction = frame.Function;
            int minDepth = _vm.CallStackFrameCount;
            Symbol startSymbol;
            
            // Get the current debug symbol. If no symbol is at the current PC, step until we find one, then break.
            if (!_debugger.GetSymbol(frame, out startSymbol)) {
                frame = _vm.GetStackFrame(0);
                while (!_debugger.GetSymbol(frame, out startSymbol)) {
                    yield return ActionRequest.STEP;
                }
                yield break;
            }
            
            // Keep stepping until we find a debug symbol on the next line, or we find a breakpoint in a new function.
            while (true) {
                yield return ActionRequest.STEP;

                frame = _vm.GetStackFrame(0);
                
                Symbol symbol;
                if (_debugger.GetSymbol(frame, out symbol)) {
                    if (frame.Function != currentFunction) {
                        break;
                    }
                    
                    if (symbol.SourceFile == startSymbol.SourceFile && symbol.Start.Line > startSymbol.Start.Line) {
                        break;
                    }
                }


                if (_vm.CallStackFrameCount < minDepth) {
                    // The frame we started stepping in has returned. Stop stepping.
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
            Symbol startSymbol;
            
            // Get the current debug symbol. If no symbol is at the current PC, step until we find one, then break.
            if (!_debugger.GetSymbol(frame, out startSymbol)) {
                frame = _vm.GetStackFrame(0);
                while (!_debugger.GetSymbol(frame, out startSymbol)) {
                    yield return ActionRequest.STEP;
                }
                yield break;
            }
            
            // Keep stepping until we find a debug symbol on the next line, or we exit out of the current function.
            while (true) {
                yield return ActionRequest.STEP;

                frame = _vm.GetStackFrame(0);

                if (frame.Function == currentFunction) {
                    Symbol symbol;
                    if (_debugger.GetSymbol(frame, out symbol)) {
                        if (symbol.SourceFile == startSymbol.SourceFile && symbol.Start.Line > startSymbol.Start.Line) {
                            break;
                        }
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
            StackFrameInfo frame = _vm.GetStackFrame(0);
            int minDepth = _vm.CallStackFrameCount;

            while (true) {
                yield return ActionRequest.STEP;
                
                if (_vm.CallStackFrameCount < minDepth) {
                    // The frame we started stepping in has returned. Stop stepping.
                    break;
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
        
        public void Pause()
        {
            _debuggableVm.SetStepping(true);
        }

        void IVMDebugger.HandleModuleAdded(VMModule module)
        {
            _debugger.AddModule(module);
        }
        
        void IVMDebugger.HandleBreak()
        {
            _isPaused = true;
            
            HandlePausedChanged();

            StackFrameInfo frame = _vm.GetStackFrame(0);
            _lastBreakWasCausedByBreakpoint = _debugger.RestoreInstruction(frame.Module, frame.Function, frame.PC);
            if (_lastBreakWasCausedByBreakpoint) {
                _debuggableVm.SetStepping(true);
            }

            while (UpdateAction(isStartingAction: false)) { }
        }
        
        private bool _isUpdating;
        private bool _didBreakImmediatley;
        private IEnumerator<object?>? _updateRequest;
        
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
                
                if (_lastBreakWasCausedByBreakpoint) {
                    // Cancel current action if last break was a breakpoint.
                    // Force functionality can easily be implemented here.
                    if (_currentAction != null) {
                        _currentAction.Dispose();
                        _currentAction = null;
                    }
                }
                
                // Restore the breakpoint for the frame we are stepping from, but don't apply breakpoints for other 
                // instructions as we might have just broken on one of those and need the instruction to be restored
                // in order to continue.
                _debugger.ReApplyBreakpoint(originFrame);
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
            _isPaused = false;
            _debuggableVm.Continue();
            _isUpdating = false;
            if (!_didBreakImmediatley) {
                HandlePausedChanged();
            }
            return _didBreakImmediatley;
        }

        void IVMDebugger.HandleFinished()
        {
            if (_currentAction != null) {
                _currentAction.Dispose();
                _currentAction = null;
            }

            if (_isPaused) {
                _isPaused = false;
                HandlePausedChanged();
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