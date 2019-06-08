using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonC.Frontend
{   
    internal class Program
    { 
        public static void Main(string[] args)
        {
            bool isInteractive = args.Contains("-i");
            bool showLex = args.Contains("--showlex");
            
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
                string line;
                StringBuilder inputBuilder = new StringBuilder();
                while ((line = Console.In.ReadLine()) != null) {
                    inputBuilder.AppendLine(line);
                }
                input = inputBuilder.ToString();
                Lex(input, lexer, tokens, verbose: showLex);
            }
            
            Parser parser = new Parser();
            List<IASTLeaf> tree = new List<IASTLeaf>();
            List<ParseError> errors = new List<ParseError>();
            parser.Parse(tokens, tree, errors);

            Console.WriteLine();
            
            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                ParseError error = errors[i];
                Console.Error.WriteLine($"{error.Token.Line},{error.Token.Column}: {error.Message}");
            }

            if (errors.Count > 0) {
                Environment.Exit(1);
            }

            PrintTreeVisitor treeVisitor = new PrintTreeVisitor();
            for (int i = 0, ilen = tree.Count; i < ilen; ++i) {
                tree[i].Accept(treeVisitor);
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