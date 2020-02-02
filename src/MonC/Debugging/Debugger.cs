using System.Collections.Generic;
using MonC.Bytecode;
using MonC.Codegen;
using MonC.VM;

namespace MonC.Debugging
{
    public class Debugger
    {
        private struct ModuleDebugData
        {
            public Dictionary<int, Dictionary<int, Instruction>> ReplacedInstructionsByFunction;
        }
        
        private readonly Dictionary<VMModule, ModuleDebugData> _debugDataByModule = new Dictionary<VMModule, ModuleDebugData>();
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        
        public void SetBreakpoint(string sourcePath, int lineNumber)
        {
            Breakpoint breakpoint = new Breakpoint {SourcePath = sourcePath, LineNumber = lineNumber};
            _breakpoints.Add(breakpoint);
            ReplaceInstructionForBreakpoint(breakpoint);
        }

        public void RemoveBreakpoint(string sourcePath, int lineNumber)
        {
            Breakpoint breakpoint = new Breakpoint {SourcePath = sourcePath, LineNumber = lineNumber};
            _breakpoints.Remove(breakpoint);
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


        public void AddModule(VMModule module)
        {
            if (_debugDataByModule.ContainsKey(module)) {
                return;
            }

            ModuleDebugData data = new ModuleDebugData {
                ReplacedInstructionsByFunction = new Dictionary<int, Dictionary<int, Instruction>>()
            };
            _debugDataByModule.Add(module, data);

            ReplaceInstructionForAllBreakpointsInModule(module);
        }

        public bool GetSymbol(StackFrameInfo frame, out Symbol symbol)
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

        private void ReplaceInstructionForAllBreakpointsInModule(VMModule module)
        {
            foreach (Breakpoint breakpoint in _breakpoints) {
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

        public void ReplaceInstruction(VMModule module, int functionIndex, int address)
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
        
        public bool RestoreInstruction(VMModule module, int functionIndex, int address)
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
        
        private void RestoreInstructionForBreakpoint(Breakpoint breakpoint)
        {
            foreach (VMModule module in _debugDataByModule.Keys) {
                int function, address;
                if (LookupSymbol(module, breakpoint.SourcePath, breakpoint.LineNumber, out function, out address)) {
                    RestoreInstruction(module, function, address);
                }
            }
        }

        public void ReApplyBreakpoint(StackFrameInfo info)
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

    }
}