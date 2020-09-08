using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonC.Debugging;
using MonC.DotNetInterop;
using MonC.Parsing;
using MonC.VM;

namespace MonC.Frontend
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            bool isInteractive = false;
            bool showLex = false;
            bool showAST = false;
            bool showIL = false;
            bool withDebugger = false;
            bool forceCodegen = false;
            bool llvm = false;
            string targetTriple = "";
            bool O0 = false;
            bool O1 = false;
            bool O2 = false;
            bool Os = false;
            bool Oz = false;
            bool O3 = false;
            bool debugInfo = false;
            bool columnInfo = false;
            List<string> positionals = new List<string>();
            List<int> argsToPass = new List<int>();
            List<string> libraryNames = new List<string>();

            for (int i = 0, ilen = args.Length; i < ilen; ++i) {
                string arg = args[i].Trim();
                bool argFound = true;

                switch (arg) {
                    case "-i":
                        isInteractive = true;
                        break;
                    case "--showlex":
                        showLex = true;
                        break;
                    case "--showast":
                        showAST = true;
                        break;
                    case "--showil":
                        showIL = true;
                        break;
                    case "-a":
                        int argToPass;
                        Int32.TryParse(args[++i], out argToPass);
                        argsToPass.Add(argToPass);
                        break;
                    case "-l":
                        libraryNames.Add(args[++i]);
                        break;
                    case "--debugger":
                        withDebugger = true;
                        break;
                    case "--force-codegen":
                        // Indicate that the compiler should attempt to code gen IL, even if the parse stage failed. 
                        // This can be helpful to diagnose generated IL for code that doesn't compile outside of a 
                        // specific project due to undefined references. The parser tries its hardest to produce a
                        // usable AST, even if there are semantic errors. Syntax errors? Not so much.
                        forceCodegen = true;
                        break;
                    case "--llvm":
                        llvm = true;
                        break;
                    case "--target":
                        targetTriple = args[++i];
                        break;
                    case "-O0":
                        O0 = true;
                        break;
                    case "-O1":
                        O1 = true;
                        break;
                    case "-O2":
                        O2 = true;
                        break;
                    case "-Os":
                        Os = true;
                        break;
                    case "-Oz":
                        Oz = true;
                        break;
                    case "-O3":
                        O3 = true;
                        break;
                    case "-g":
                        debugInfo = true;
                        break;
                    case "-gcolumn-info":
                        columnInfo = true;
                        break;
                    default:
                        argFound = false;
                        break;
                }

                if (!argFound) {
                    if (!arg.Contains("-")) {
                        positionals.Add(arg);
                    }
                }
            }

            InteropResolver interopResolver = new InteropResolver();

            foreach (string libraryName in libraryNames) {
                Assembly lib = Assembly.LoadFile(Path.GetFullPath(libraryName));
                interopResolver.ImportAssembly(lib,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
            }

            ParseModule interopHeaderModule = interopResolver.CreateHeaderModule();

            string? filename = null;

            if (positionals.Count > 0) {
                filename = positionals[0];
            }

            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();

            string? input;

            if (isInteractive) {
                WritePrompt();
                while ((input = Console.ReadLine()) != null) {
                    LexLine(input, lexer, tokens, verbose: showLex);
                    WritePrompt();
                }
            } else {
                if (filename == null) {
                    while ((input = Console.In.ReadLine()) != null) {
                        LexLine(input, lexer, tokens, verbose: showLex);
                    }
                } else {
                    filename = Path.GetFullPath(filename);
                    using StreamReader reader = new StreamReader(filename);
                    while ((input = reader.ReadLine()) != null) {
                        LexLine(input, lexer, tokens, verbose: showLex);
                    }
                }
            }

            lexer.FinishLex(tokens);

            Parser parser = new Parser();
            List<ParseError> errors = new List<ParseError>();
            ParseModule module = parser.Parse(filename, tokens, interopHeaderModule, errors);

            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                ParseError error = errors[i];
                Console.Error.WriteLine($"{error.Start.Line + 1},{error.Start.Column + 1}: {error.Message}");
            }

            if (showAST) {
                PrintTreeVisitor treeVisitor = new PrintTreeVisitor();
                for (int i = 0, ilen = module.Functions.Count; i < ilen; ++i) {
                    module.Functions[i].Accept(treeVisitor);
                }
            }

            if (errors.Count > 0 && !forceCodegen) {
                Environment.Exit(1);
            }

            if (!llvm) {
                Codegen.CodeGenerator generator = new Codegen.CodeGenerator();
                Codegen.ILModule ilmodule = generator.Generate(module);
                if (showIL) {
                    ilmodule.WriteListing(Console.Out);
                }

                if (errors.Count > 0) {
                    Environment.Exit(1);
                }

                List<LinkError> linkErrors = new List<LinkError>();
                Linker linker = new Linker(linkErrors);
                linker.AddModule(ilmodule, export: true);

                foreach (Binding binding in interopResolver.Bindings) {
                    linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation, export: false);
                }

                VMModule vmModule = linker.Link();

                if (linkErrors.Count > 0) {
                    foreach (LinkError error in linkErrors) {
                        Console.Error.WriteLine($"Link error: {error.Message}");
                    }

                    Environment.Exit(1);
                }

                List<string> loadErrors = new List<string>();
                if (!interopResolver.PrepareForExecution(vmModule, loadErrors)) {
                    foreach (string error in loadErrors) {
                        Console.Error.WriteLine($"Load error: {error}");
                    }

                    Environment.Exit(1);
                }

                VirtualMachine vm = new VirtualMachine();

                if (withDebugger) {
                    Debugger debugger = new Debugger();
                    VMDebugger vmDebugger = new VMDebugger(debugger, vm);
                    vmDebugger.Break += () => HandleBreak(vm, debugger, vmDebugger);
                    vmDebugger.Pause();
                }

                if (!vm.Call(vmModule, "main", argsToPass, success => HandleExecutionFinished(vm, success))) {
                    Console.Error.WriteLine("Failed to call main function.");
                    Environment.Exit(-1);
                }
            } else {
                using LLVM.PassManagerBuilder? optBuilder =
                    O0 || O1 || O2 || Os || Oz || O3 ? new LLVM.PassManagerBuilder() : null;
                LLVM.CAPI.LLVMCodeGenOptLevel optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.None;
                if (optBuilder != null) {
                    if (O0) {
                        optBuilder.SetOptLevels(0, 0);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.None;
                    } else if (O1) {
                        optBuilder.SetOptLevels(1, 0);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.Less;
                    } else if (O2) {
                        optBuilder.SetOptLevels(2, 0);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.Default;
                    } else if (Os) {
                        optBuilder.SetOptLevels(2, 1);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.Default;
                    } else if (Oz) {
                        optBuilder.SetOptLevels(2, 2);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.Default;
                    } else if (O3) {
                        optBuilder.SetOptLevels(3, 0);
                        optLevel = LLVM.CAPI.LLVMCodeGenOptLevel.Aggressive;
                    }
                }

                using (LLVM.Context llvmContext = new LLVM.Context()) {
                    if (targetTriple.Length == 0)
                        targetTriple = LLVM.Target.DefaultTargetTriple;
                    targetTriple = LLVM.Target.NormalizeTargetTriple(targetTriple);
                    using (LLVM.Module llvmModule =
                        LLVM.CodeGenerator.Generate(llvmContext, filename ?? "<stdin>", module, targetTriple,
                            optBuilder, debugInfo, columnInfo)) {
                        if (showIL) {
                            llvmModule.Dump();
                        }

                        LLVM.Target.InitializeAllTargets();
                        LLVM.Target target = LLVM.Target.FromTriple(targetTriple);
                        using LLVM.TargetMachine targetMachine = target.CreateTargetMachine(
                            targetTriple, "", "", optLevel, LLVM.CAPI.LLVMRelocMode.Default,
                            LLVM.CAPI.LLVMCodeModel.Default);
                        targetMachine.SetAsmVerbosity(true);
                        targetMachine.EmitToFile(llvmModule, "-", LLVM.CAPI.LLVMCodeGenFileType.AssemblyFile);
                    }
                }
            }
        }

        private static void HandleExecutionFinished(VirtualMachine vm, bool success)
        {
            if (!success) {
                Environment.Exit(-1);
            }

            Environment.Exit(vm.ReturnValue);
        }

        private static void WritePrompt()
        {
            Console.Write("> ");
            Console.Out.Flush();
        }

        private static void LexLine(string input, Lexer lexer, List<Token> tokens, bool verbose)
        {
            int firstTokenIdx = tokens.Count;
            lexer.LexLine(input, tokens);

            if (verbose) {
                for (int i = firstTokenIdx, ilen = tokens.Count; i < ilen; ++i) {
                    Console.WriteLine(tokens[i]);
                }
            }
        }

        private static void HandleBreak(VirtualMachine vm, Debugger debugger, VMDebugger vmDebugger)
        {
            while (DebuggerLoop(vm, debugger, vmDebugger)) {
            }
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
                    string? sourcePath;
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
                    string? sourcePath;
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