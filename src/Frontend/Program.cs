using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MonC.Parsing;

namespace MonC.Frontend
{   
    internal class Program
    { 
        public static void Main(string[] args)
        {
            bool isInteractive = args.Contains("-i");
            bool showLex = args.Contains("--showlex");

            string filename = null;
            
            for (int i = 0, ilen = args.Length; i < ilen; ++i) {
                string arg = args[i];
                if (!arg.StartsWith("-")) {
                    filename = arg;
                    break;
                }
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

            PrintTreeVisitor treeVisitor = new PrintTreeVisitor();
            for (int i = 0, ilen = module.Functions.Count; i < ilen; ++i) {
                module.Functions[i].Accept(treeVisitor);
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
                for (int i = 0, ilen = tokens.Count; i < ilen; ++i) {
                    Console.WriteLine(tokens[i]);
                }    
            }
            tokens.AddRange(newTokens);
        }
    }
}