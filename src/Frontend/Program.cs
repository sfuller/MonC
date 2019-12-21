using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using MonC.Codegen;
using MonC.DotNetInterop;
using MonC.Parsing;
using MonC.VM;
using Debugger = MonC.Debugging.Debugger;

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
                interopResolver.ImportAssembly(lib, BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
            }

            ParseModule interopHeaderModule = interopResolver.CreateHeaderModule(); 
            
            string? filename = null;

            if (positionals.Count > 0) {
                filename = positionals[0];
            }

            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            
            string input;

            if (isInteractive) {
                WritePrompt();
                while ((input = Console.ReadLine()) != null) {
                    Lex(input, lexer, tokens, verbose: showLex);
                    Lex("\n", lexer, tokens, verbose: showLex);
                    WritePrompt();
                }    
            } else {
                if (filename == null) {
                    string? line;
                    StringBuilder inputBuilder = new StringBuilder();
                    while ((line = Console.In.ReadLine()) != null) {
                        inputBuilder.AppendLine(line);
                    }
                    input = inputBuilder.ToString();    
                } else {
                    filename = Path.GetFullPath(filename);
                    input = File.ReadAllText(filename);
                }
                
                Lex(input, lexer, tokens, verbose: showLex);
            }
            
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
            
            CodeGenerator generator = new CodeGenerator();
            ILModule ilmodule = generator.Generate(module);
            if (showIL) {
                IntermediateLanguageWriter writer = new IntermediateLanguageWriter(Console.Out);
                writer.Write(ilmodule);    
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
            vm.LoadModule(vmModule);
            
            if (withDebugger) {
                Debugger debugger = new Debugger(vmModule, vm);
                debugger.Break += () => HandleBreak(vm, debugger);
                debugger.Pause();
            }

            if (!vm.Call("main", argsToPass, success => HandleExecutionFinished(vm, success))) {
                Console.Error.WriteLine("Failed to call main function.");
                Environment.Exit(-1);
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

        private static void Lex(string input, Lexer lexer, List<Token> tokens, bool verbose)
        {
            List<Token> newTokens = new List<Token>();
            lexer.Lex(input, newTokens);

            if (verbose) {
                for (int i = 0, ilen = newTokens.Count; i < ilen; ++i) {
                    Console.WriteLine(newTokens[i]);
                }    
            }
            tokens.AddRange(newTokens);
        }

        private static void HandleBreak(VirtualMachine vm, Debugger debugger)
        {
            while (DebuggerLoop(vm, debugger)) {}
        }

        private static bool DebuggerLoop(VirtualMachine vm, Debugger debugger)
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
                case "reg": 
                    {
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

                case "bp":
                    {
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
                        bool success = debugger.SetBreakpoint(sourcePath!, breakpointLineNumber - 1);
                        if (!success) {
                            Console.WriteLine("Could not set breakpoint");
                        }
                    } 
                    break;
                
                case "over":
                    return debugger.StepOver();

                case "into":
                    return debugger.StepInto();

                case "out":
                    return debugger.StepOut();

                case "step":
                    return debugger.Step();

                case "continue":
                case null:
                    return debugger.Continue();

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