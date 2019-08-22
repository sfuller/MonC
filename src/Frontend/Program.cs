using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonC.Codegen;
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
            List<string> positionals = new List<string>();
            List<int> argsToPass = new List<int>();

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
                    input = File.ReadAllText(filename);
                }
                
                Lex(input, lexer, tokens, verbose: showLex);
            }
            
            Parser parser = new Parser();
            Module module = new Module();
            List<ParseError> errors = new List<ParseError>();
            parser.Parse(tokens, module, errors);

            Console.WriteLine();
            
            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                ParseError error = errors[i];
                Console.Error.WriteLine($"{error.Token.Line},{error.Token.Column}: {error.Message}");
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
            
            VirtualMachine vm = new VirtualMachine();
            vm.LoadModule(ilmodule);
            vm.Call("main", argsToPass);
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
    }
}