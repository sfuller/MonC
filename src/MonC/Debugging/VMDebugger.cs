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
            // Keep stepping until we find a debug symbol.
            while (_vm.IsRunning) {
                yield return ActionRequest.STEP;
                
                if (_lastBreakWasCausedByBreakpoint) {
                    // If we hit a breakpoint, stop attemping to step into
                    // Unless, of cource, we implement 'force' functionality.
                    break;
                }
                
                StackFrameInfo frame = _vm.GetStackFrame(0);
                if (_debugger.GetSymbol(frame, out _)) {
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
                    if (_debugger.GetSymbol(frame, out _)) {
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
                _debugger.ReplaceInstruction(previousFrame.Module, previousFrame.Function, previousFrame.PC);
            }
            
            yield return ActionRequest.CONTINUE;
            
            if (_lastBreakWasCausedByBreakpoint) {
                // When we continued, a breakpoint was hit. That was either our transient breakpoint we just set or
                // some other breakpoint. No matter the case, we shouldn't continue stepping out 
                // (unless in the future we imlement a 'force' step out, like IntelliJ has)

                // Try removing the transient breakpoint we just set incase it wasn't hit, so it isn't hit later.
                if (stackFrameCount > 1) {
                    _debugger.RestoreInstruction(previousFrame.Module, previousFrame.Function, previousFrame.PC);
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
            _debuggableVm.Continue();
            _isUpdating = false;
            if (!_didBreakImmediatley) {
                HandlePausedChanged();
            }
            return _didBreakImmediatley;
        }

        void IVMDebugger.HandleFinished()
        {
            // TODO: Need to restore all replaced instructions!
            // TODO: This only needs to be done if the module can be re-used by other debuggers.

            HandlePausedChanged();
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