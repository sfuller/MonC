using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MonC.Codegen;
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
            //AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
            //    if (eventArgs.ExceptionObject is Exception exception) {
            //        Console.Error.WriteLine(exception.ToString());
            //        Console.Error.WriteLine(new StackTrace(exception));
            //        Environment.Exit(2);    
            //    }
            //}; 
            
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
            
            string filename = null;

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
                    string line;
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
                Console.Error.WriteLine($"{error.Token.Line + 1},{error.Token.Column + 1}: {error.Message}");
            }

            if (errors.Count > 0) {
                Environment.Exit(1);
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
                IntermediateLanguageWriter writer = new IntermediateLanguageWriter();
                writer.Write(ilmodule, Console.Out);    
            }
            
            Linker linker = new Linker();
            linker.AddModule(ilmodule);

            foreach (Binding binding in interopResolver.Bindings) {
                linker.AddFunctionBinding(binding.Prototype.Name, binding.Implementation);
            }

            List<LinkError> linkErrors = new List<LinkError>();
            VMModule vmModule = linker.Link(linkErrors);

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

            Debugger debugger = null;
            if (withDebugger) {
                debugger = new Debugger();
                debugger.Setup(vmModule, vm);
                debugger.Pause();
            }

            vm.Call("main", argsToPass, start: !withDebugger);

            if (debugger != null) {
                while (vm.IsRunning) {
                    DebuggerLoop(vm, debugger);
                }
            }
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

        private static void DebuggerLoop(VirtualMachine vm, Debugger debugger)
        {
            Console.Write("(moncdbg) ");

            string line = Console.ReadLine();
            if (line != null) {
                line = line.Trim();    
            }

            switch (line) {
                case "pc":
                    StackFrameInfo frame = vm.GetStackFrame(0);
                    Console.WriteLine($"Function: {frame.Function}, PC: {frame.PC}");
                    break;
                
                case "next":
                    debugger.StepNext();
                    break;
                
                case "into":
                    debugger.StepInto();
                    break;
                
                case "continue":
                case null:
                    debugger.Continue();
                    break;
                
                case "":
                    break;
                
                default:
                    Console.Error.WriteLine($"moncdbg: unknown command {line}");
                    break;
            }
        }
    }
}