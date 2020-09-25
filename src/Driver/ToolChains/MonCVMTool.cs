using System;
using System.IO;
using MonC.Debugging;
using MonC.VM;

namespace Driver.ToolChains
{
    internal class MonCFinishedException : Exception
    {
        public int ReturnValue { get; }

        public MonCFinishedException(int returnValue) => ReturnValue = returnValue;
    }

    public class MonCVMTool : IExecutableTool
    {
        protected Job _job;
        protected IVMInput _input;

        protected MonCVMTool(Job job, IVMInput input)
        {
            _job = job;
            _input = input;
        }

        public static MonCVMTool Construct(Job job, MonC toolchain, IVMInput input)
        {
            if (job._debugger)
                return new MonCDebuggerVMTool(job, input);
            return new MonCVMTool(job, input);
        }

        public virtual void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCVMTool");
        }

        protected virtual void SetUpDebugger(VirtualMachine vm) { }

        public int Execute()
        {
            VirtualMachine vm = new VirtualMachine();

            SetUpDebugger(vm);

            try {
                if (!vm.Call((VMModule) _input.GetVMModuleArtifact(), _job._entry, _job._argsToPass,
                    success => HandleExecutionFinished(vm, success))) {
                    throw Diagnostics.ThrowError($"Failed to call '{_job._entry}' function.");
                }
            } catch (MonCFinishedException exception) {
                return exception.ReturnValue;
            }

            return vm.ReturnValue;
        }

        private static void HandleExecutionFinished(VirtualMachine vm, bool success)
        {
            throw new MonCFinishedException(success ? vm.ReturnValue : 1);
        }
    }

    public class MonCDebuggerVMTool : MonCVMTool
    {
        internal MonCDebuggerVMTool(Job job, IVMInput input) : base(job, input) { }

        public override void WriteInputChain(TextWriter writer)
        {
            _input.WriteInputChain(writer);
            writer.WriteLine("  -MonCDebuggerVMTool");
        }

        protected override void SetUpDebugger(VirtualMachine vm)
        {
            Debugger debugger = new Debugger();
            VMDebugger vmDebugger = new VMDebugger(debugger, vm);
            vmDebugger.Break += () => HandleBreak(vm, debugger, vmDebugger);
            vmDebugger.Pause();
        }

        private static void HandleBreak(VirtualMachine vm, Debugger debugger, VMDebugger vmDebugger)
        {
            while (DebuggerLoop(vm, debugger, vmDebugger)) { }
        }

        private static bool DebuggerLoop(VirtualMachine vm, Debugger debugger, VMDebugger vmDebugger)
        {
            Console.Write("(moncdbg) ");

            string line = Console.ReadLine();
            string[] args;
            if (line != null) {
                args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            } else {
                args = Array.Empty<string>();
            }

            string command = "";
            if (args.Length > 0) {
                command = args[0];
            }

            switch (command) {
                case "reg": {
                    StackFrameInfo frame = vm.GetStackFrame(0);
                    Console.WriteLine($"Function: {frame.Function}, PC: {frame.PC}, A: {vm.ReturnValue}");
                    string sourcePath;
                    int lineNumber;
                    if (debugger.GetSourceLocation(frame, out sourcePath, out lineNumber)) {
                        Console.WriteLine($"File: {sourcePath}, Line: {lineNumber + 1}");
                    }
                }
                    break;

                case "read":
                    StackFrameMemory memory = vm.GetStackFrameMemory(0);
                    for (int i = 0, ilen = memory.Size; i < ilen; ++i) {
                        if (i % 4 == 0 && i != 0) {
                            Console.WriteLine();
                        }

                        Console.Write(memory.Read(i) + "\t");
                    }

                    Console.WriteLine();
                    break;

                case "bp": {
                    if (args.Length < 2) {
                        Console.WriteLine("Not enough args");
                        break;
                    }

                    int breakpointLineNumber;
                    int.TryParse(args[1], out breakpointLineNumber);
                    StackFrameInfo frame = vm.GetStackFrame(0);
                    string sourcePath;
                    if (!debugger.GetSourceLocation(frame, out sourcePath, out _)) {
                        sourcePath = "";
                    }

                    Console.WriteLine($"Assuming source file is {sourcePath}");
                    debugger.SetBreakpoint(sourcePath!, breakpointLineNumber - 1);
                }
                    break;

                case "over":
                    return vmDebugger.StepOver();

                case "into":
                    return vmDebugger.StepInto();

                case "out":
                    return vmDebugger.StepOut();

                case "step":
                    return vmDebugger.Step();

                case "continue":
                case null:
                    return vmDebugger.Continue();

                case "":
                    break;

                default:
                    Console.Error.WriteLine($"moncdbg: unknown command {line}");
                    break;
            }

            return true;
        }
    }
}
