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
            List<string> errors = new List<string>();
            parser.Parse(tokens, tree, errors);

            Console.WriteLine();
            
            for (int i = 0, ilen = errors.Count; i < ilen; ++i) {
                Console.Error.WriteLine(errors[i]);
            }

            for (int i = 0, ilen = tree.Count; i < ilen; ++i) {
                Console.WriteLine(tree[i]);
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