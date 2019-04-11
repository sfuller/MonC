using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MonC;

namespace LexerFrontend
{   
    internal class Program
    { 
        public static void Main(string[] args)
        {
            Lexer lexer = new Lexer();
            List<Token> tokens = new List<Token>();
            
            WritePrompt();

            string input;
            
            while ((input = Console.ReadLine()) != null) {
                Lex(input, lexer, tokens);
                Lex("\n", lexer, tokens);
                WritePrompt();
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

        private static void Lex(string input, Lexer lexer, List<Token> tokens)
        {
            List<Token> newTokens = new List<Token>();
            lexer.Lex(input, newTokens);
            
            for (int i = 0, ilen = tokens.Count; i < ilen; ++i) {
                Console.WriteLine(tokens[i]);
            }
            
            tokens.AddRange(newTokens);
        }
    }
}